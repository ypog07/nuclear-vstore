using AmsMigrator.ImportStrategies;
using AmsMigrator.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

using AmsMigrator.Infrastructure;

using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using SixLabors.ImageSharp;
using System.IO;
using AmsMigrator.Helpers;

using Serilog.Context;

namespace AmsMigrator
{
    public class AdvertisementMaterialsImporter
    {
        private readonly IErmDbClient _dbClient;
        private readonly ImportOptions _options;
        private readonly IAmsClient _amsClient;
        protected readonly IOkapiClient _okapiClient;
        private readonly IServiceProvider _container;
        private readonly ILogger _logger = Log.Logger;
        private readonly HashSet<long> _kbFirms;
        private readonly HashSet<long> _zmkFirms;
        private readonly ConcurrentQueue<Amsv1MaterialData> _deferredQueue;
        private readonly ConcurrentQueue<(long[] Batch, int RetryAttempt)> _fetchingQueue;
        private int _processedAdCounter;
        private int _processedLogoCounter;
        private int _processedKbCounter;
        private int _processedZmkCounter;
        private int _bindedOrdersCounter;
        private int _fetchedAmCounter;
        private int _currentBatchCounter;
        private double _totalBatchesCount;
        private readonly Timer _speedEstimatingTimer;
        private int _speedEstimatingCounter;

        public AdvertisementMaterialsImporter(ImportOptions options, IAmsClient amsClient, IOkapiClient okapiClient, IErmDbClient dbClient, IServiceProvider container)
        {
            _amsClient = amsClient;
            _okapiClient = okapiClient;
            _options = options;
            _dbClient = dbClient;
            _container = container;
            _deferredQueue = new ConcurrentQueue<Amsv1MaterialData>();
            _fetchingQueue = new ConcurrentQueue<(long[] Batch, int RetryAttempt)>();
            _speedEstimatingTimer = new Timer(ReportMigrationPace, null, 0, 60000);

            Stats.Collector["fetch"] = 0;
            Stats.Collector["import"] = 0;
            Stats.Collector["imgProc"] = 0;
            Stats.Collector["binding"] = 0;
            Stats.Collector["full"] = 0;

            _kbFirms = _dbClient.GetFirmIdsForGivenPositions(_options.ThresholdDate, _options.KbLogoTriggerNomenclatures).ToHashSet();
            _zmkFirms = _dbClient.GetFirmIdsForGivenPositions(_options.ThresholdDate, _options.ZmkLogoTriggerNomenclatures).ToHashSet();
        }

        public double TotalBatchesCount => _totalBatchesCount;

        public async Task StartImportAsync(string[] adUuids)
        {
            var partitions = adUuids
                            .Select((x, i) => new { Index = i, Value = x })
                            .GroupBy(x => x.Index / _options.BatchSize)
                            .Select(x => x.Select(v => v.Value).ToList());

            foreach (var part in partitions)
            {
                var partWithCommit = part.Select(e => (e, "HEAD")).ToArray();
                var amsv1Data = await _amsClient.GetLogoInfoByUidAsync(partWithCommit, true);

                Parallel.ForEach(amsv1Data,
                            new ParallelOptions { MaxDegreeOfParallelism = 4 }, n => PatchSourceImportData(n, true));

                if (amsv1Data.Length == 0)
                    continue;

                if (_options.ParallelImportEnabled)
                {
                    var importTasks = amsv1Data.Select(PerformImportForSingleData);
                    var bindingData = await Task.WhenAll(importTasks);
                    await RunBindingWorker(bindingData.SelectMany(n => n).ToList());
                }
                else
                {
                    foreach (var datum in amsv1Data)
                    {
                        var bindingData = await PerformImportForSingleData(datum);
                        await RunBindingWorker(bindingData);
                    }
                }

                while (_deferredQueue.TryDequeue(out var ad))
                {
                    var bindingData = await PerformImportForSingleData(ad);
                    await RunBindingWorker(bindingData);

                    _logger.Information("{length} elements remaining in queue.", _deferredQueue.Count);
                }

                DumpDeferredQueue();
            }
        }

        public async Task<int> StartImportAsync(IProgress<double> progress = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                long startAfterFirmId = _options.StartAfterFirmId ?? 0;
                int testModeLimit = _options.TestMode.Limit;

                var firms = await _dbClient.GetAdvertiserFirmIdsAsync(_options.ThresholdDate);

                if (_options.TestMode.Enabled)
                {
                    firms = firms.Take(testModeLimit).ToList();
                }

                if (_options.StartAfterFirmId != null)
                {
                    firms = firms.OrderBy(n => n).SkipWhile(f => f <= startAfterFirmId).ToList();
                    _totalBatchesCount = CalculateBatchCount(firms.Count);
                }

                _logger.Verbose($"All (Company logo) advertiser ids: {string.Join(",", firms.Select(i => i.ToString()))}");
                _logger.Verbose($"KB advertiser ids: {string.Join(",", _kbFirms.Select(i => i.ToString()))}");
                _logger.Verbose($"ZMK advertiser ids: {string.Join(",", _zmkFirms.Select(i => i.ToString()))}");

                if (_options.Mode == AdExportMode.OnePerTime)
                {
                    foreach (var firmId in firms)
                    {
                        if (startAfterFirmId > 0 && firmId <= startAfterFirmId)
                        {
                            continue;
                        }

                        var amsv1Data = await _amsClient.GetAmsv1DataAsync(firmId, true);

                        PatchSourceImportData(amsv1Data, true);

                        if (amsv1Data == null)
                        {
                            continue;
                        }

                        _logger.Information("Starting to process AMS 1.0 material with uuid: {uuid}", amsv1Data.Uuid);
                        _logger.Information("Processing logo image: {image}", amsv1Data);

                        var bindingData = await PerformImportForSingleData(amsv1Data);
                        await RunBindingWorker(bindingData);
                    }
                }
                else
                if (_options.Mode == AdExportMode.Batch)
                {
                    _totalBatchesCount = CalculateBatchCount(firms.Count);

                    await RunBatchedImport(firms, progress);
                }

                _logger.Information("Executing retry queue. Initial queue length {length}", _deferredQueue.Count);

                while (_deferredQueue.TryDequeue(out var ad))
                {
                    var bindingData = await PerformImportForSingleData(ad);
                    await RunBindingWorker(bindingData);

                    _logger.Information("[RETRY_QUEUE_LENGTH] {length} elements remaining in queue.", _deferredQueue.Count);
                }

                sw.Stop();

                DumpDeferredQueue();

                return _processedLogoCounter + _processedKbCounter + _processedZmkCounter;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException ae)
                {
                    ex = ae.Flatten();
                }

                _logger.Fatal(ex, "Oops! Unhandled exception occured. Application will be stopped. Bye-bye :(");
                throw;
            }
            finally
            {
                _logger.Information("===== MIGRATION STATISTICS =====");
                _logger.Information("Db instance key: {key}", _options.DbInstanceKey);
                _logger.Information("Fetch time: {time} ms", Stats.Collector["fetch"]);
                _logger.Information("Img processing time: {time} ms", Stats.Collector["imgProc"]);
                _logger.Information("Import time: {time} ms", (Stats.Collector["import"]));
                _logger.Information("Binding time: {time} ms", Stats.Collector["binding"]);

                _logger.Information("Processed AMS 1.0 advertisements: {0}", _processedAdCounter);
                _logger.Information("AMs imported to target system (Logo): {0}", _processedLogoCounter);
                _logger.Information("AMs imported to target system (KB): {0}", _processedKbCounter);
                _logger.Information("AMs imported to target system (ZMK): {0}", _processedZmkCounter);
                _logger.Information("AMs to orders binded: {0}", _bindedOrdersCounter);
                _logger.Information("AMs fetched from source system: {0}", _fetchedAmCounter);
                _logger.Information("Execution time: {0}", sw.Elapsed);
                _logger.Information("Average import pace: {n:0.00} ad/s", _processedAdCounter / (sw.ElapsedMilliseconds / 1000d));

                _speedEstimatingTimer.Dispose();

                if (_options.TestMode.Enabled)
                {
                    _logger.Information("Test mode execution is finished. Test data size is: {size}", _options.TestMode.Limit);
                }
            }
        }

        private double CalculateBatchCount(int logo)
        {
            var firmsCount = Math.Ceiling(logo / (double)_options.BatchSize);
            return firmsCount;
        }

        private async Task RunBatchedImport(IEnumerable<long> firmIds, IProgress<double> progress)
        {
            var batches = firmIds
                          .Select((x, i) => new { Index = i, Value = x })
                          .GroupBy(x => x.Index / _options.BatchSize)
                          .Select(x => x.Select(v => v.Value).ToArray());

            foreach (var batch in batches)
            {
                var fetchMiniImages = batch.ContainsElementOfHashset(_zmkFirms);

                var amsv1Data = await RunFetchingWorker(batch, 0, fetchMiniImages);

                var bindingData = await RunImportWorker(amsv1Data);
                await RunBindingWorker(bindingData);

                _currentBatchCounter++;
                progress.Report(_currentBatchCounter);
            }

            while (_fetchingQueue.TryDequeue(out var item))
            {
                var attempt = item.RetryAttempt += 1;
                var fetchMiniImages = item.Batch.ContainsElementOfHashset(_zmkFirms);

                var amsv1Data = await RunFetchingWorker(item.Batch, attempt, fetchMiniImages);
                var bindingData = await RunImportWorker(amsv1Data);
                await RunBindingWorker(bindingData);

                _currentBatchCounter++;
                progress.Report(_currentBatchCounter);

                _logger.Information("{length} elements remaining in fetching queue.", _fetchingQueue.Count);
            }
        }

        private async Task<Amsv1MaterialData[]> RunFetchingWorker(long[] part, int retryAttempt, bool miniImagesNeeded)
        {
            var sw = new Stopwatch();

            try
            {
                sw.Start();
                var amsv1Data = await _amsClient.GetAmsv1DataAsync(part, miniImagesNeeded);
                sw.Stop();
                Stats.Collector["fetch"] += sw.ElapsedMilliseconds;
                sw.Reset();
                Interlocked.Add(ref _fetchedAmCounter, amsv1Data.Length);

                sw.Start();
                Parallel.ForEach(amsv1Data,
                                 new ParallelOptions { MaxDegreeOfParallelism = 4 },
                                 n => PatchSourceImportData(n, !_zmkFirms.Contains(n.FirmId)));
                sw.Stop();
                Stats.Collector["imgProc"] += sw.ElapsedMilliseconds;
                sw.Reset();

                return amsv1Data;
            }
            catch (Exception ex)
            {
                if (retryAttempt > _options.QueueTriesCount)
                {
                    _logger.Fatal(ex, "[FETCHING_FAIL] Unable to fetch batch from AMS 1.0 {batch}", string.Join(", ", part));
                    throw;
                }

                _logger.Error(ex, "[FETCHING_FAIL] An exception occurred while fetching batch from AMS 1.0. Attempt: {retryAttempt}", retryAttempt);
                _fetchingQueue.Enqueue((part, retryAttempt));
                _currentBatchCounter--;

                return Enumerable.Empty<Amsv1MaterialData>().ToArray();
            }
        }

        private async Task<List<MaterialCreationResult>> RunImportWorker(Amsv1MaterialData[] amsv1Data)
        {
            var sw = new Stopwatch();

            var orderBindingDataList = new List<MaterialCreationResult>();

            sw.Start();
            if (_options.ParallelImportEnabled)
            {
                var importTasks = amsv1Data.Select(PerformImportForSingleData);
                var bindingData = await Task.WhenAll(importTasks);
                orderBindingDataList = bindingData.SelectMany(s => s).ToList();
            }
            else
            {
                foreach (var datum in amsv1Data)
                {
                    var bindingData = await PerformImportForSingleData(datum);
                    if (bindingData.Any())
                    {
                        orderBindingDataList.AddRange(bindingData);
                    }
                }
            }
            sw.Stop();
            Stats.Collector["import"] += sw.ElapsedMilliseconds;
            sw.Reset();

            return orderBindingDataList;
        }

        private async Task RunBindingWorker(IEnumerable<MaterialCreationResult> orderBindingDataList)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (_options.MaterialOrderBindingEnabled)
            {
                var count = await _dbClient.BindMaterialToOrderAsync(orderBindingDataList);
                Interlocked.Add(ref _bindedOrdersCounter, count);
            }
            sw.Stop();
            Stats.Collector["binding"] += sw.ElapsedMilliseconds;
            sw.Reset();
        }

        private void PatchSourceImportData(Amsv1MaterialData amsv1Data, bool isBackgroundFillingNeeded)
        {
            var (data, ext) = ImageProcessor.FillImageBackground(amsv1Data.ImageData, isBackgroundFillingNeeded ? amsv1Data.BackgroundColor : null);
            amsv1Data.ImageData = data;

            amsv1Data.ImageExt = ext ?? amsv1Data.ImageExt;
        }

        private async Task<IEnumerable<MaterialCreationResult>> PerformImportForSingleData(Amsv1MaterialData data)
        {
            try
            {
                _logger.Information("Starting to import AMS 1.0 material with uuid: {uuid}, firm: {firmId}", data.Uuid, data.FirmId);
                _logger.Information("Processing logo image: {image}", data);

                if (!data.ReachedTarget.HasFlag(ImportTarget.CompanyLogotypes))
                {
                    var logoImportStrategy = _container.GetService<CompanyLogoImportStrategy>();

                    var creationResult = await logoImportStrategy.ExecuteAsync(data);
                    if (creationResult != null)
                    {
                        data.MaterialCreationData[ImportTarget.CompanyLogotypes] = creationResult;
                    }

                    data.CompleteCreation(ImportTarget.CompanyLogotypes);

                    Interlocked.Increment(ref _processedLogoCounter);
                }

                if (!data.ModerationCompletedTarget.HasFlag(ImportTarget.CompanyLogotypes))
                {
                    await SetModerationStatus(data, data.MaterialCreationData[ImportTarget.CompanyLogotypes]);

                    data.CompleteModeration(ImportTarget.CompanyLogotypes);
                }

                if (data.HasSizeSpecificImages && _zmkFirms.Contains(data.FirmId))
                {
                    if (!data.ReachedTarget.HasFlag(ImportTarget.ZmkBrendingLogotypes))
                    {
                        var zmkStrategy = _container.GetService<ZmkLogoImportStrategy>();

                        var creationResult = await zmkStrategy.ExecuteAsync(data);
                        if (creationResult != null)
                        {
                            data.MaterialCreationData[ImportTarget.ZmkBrendingLogotypes] = creationResult;
                        }

                        data.CompleteCreation(ImportTarget.ZmkBrendingLogotypes);

                        Interlocked.Increment(ref _processedZmkCounter);
                    }

                    if (!data.ModerationCompletedTarget.HasFlag(ImportTarget.ZmkBrendingLogotypes))
                    {
                        await SetModerationStatus(data, data.MaterialCreationData[ImportTarget.ZmkBrendingLogotypes]);

                        data.CompleteModeration(ImportTarget.ZmkBrendingLogotypes);
                    }
                }

                if (_kbFirms.Contains(data.FirmId))
                {
                    if (!data.ReachedTarget.HasFlag(ImportTarget.KbLogotypes))
                    {
                        var kbLogoStrategy = _container.GetService<KBLogoImportStrategy>();

                        var creationResult = await kbLogoStrategy.ExecuteAsync(data);
                        if (creationResult != null)
                        {
                            data.MaterialCreationData[ImportTarget.KbLogotypes] = creationResult;
                        }

                        data.CompleteCreation(ImportTarget.KbLogotypes);

                        Interlocked.Increment(ref _processedKbCounter);
                    }

                    if (!data.ModerationCompletedTarget.HasFlag(ImportTarget.KbLogotypes))
                    {
                        await SetModerationStatus(data, data.MaterialCreationData[ImportTarget.KbLogotypes]);

                        data.CompleteModeration(ImportTarget.KbLogotypes);
                    }
                }

                Interlocked.Increment(ref _processedAdCounter);
                Interlocked.Increment(ref _speedEstimatingCounter);

                _logger.Information("[IMPORT_SUCCESS] AMS 1.0 material with uuid: {uuid} {firmid} has been imported successfully", data.Uuid, data.FirmId);

                return data.MaterialCreationData.Values;
            }
            catch
            {
                if (data.QueueTries != 0)
                {
                    _deferredQueue.Enqueue(data);
                    data.QueueTries -= 1;
                    _logger.Warning("[MOVE_TO_RETRY_QUEUE] Skip to process advertisement {uuid} {firmid} due to irresistible problem.", data.Uuid, data.FirmId);
                }
                else
                {
                    _logger.Error("[SKIP_AD_IMPORT] Skip to process advertisement {uuid} {firmid} due to irresistible problem.", data.Uuid, data.FirmId);
                }
                return new List<MaterialCreationResult>();
            }
        }

        private async Task SetModerationStatus(Amsv1MaterialData amsv1Data, MaterialCreationResult creationResult)
        {
            if (IsMigrationNeededForMaterial(amsv1Data))
            {
                _logger.Information("Migrating moderation info for material {stubId} version {versionId} started", creationResult.MaterialId, creationResult.VersionId);
                await _okapiClient.SetModerationState(creationResult.MaterialId, creationResult.VersionId, amsv1Data);
                _logger.Information("Migrating moderation info for material {stubId} version {versionId} completed", creationResult.MaterialId, creationResult.VersionId);
            }
        }

        private bool IsMigrationNeededForMaterial(Amsv1MaterialData amsv1Data)
        {
            return _options.StatusesForMigration.Any(s => s.Name.Equals(amsv1Data.ModerationState) && s.MigrationNeeded);
        }

        private void ReportMigrationPace(object obj)
        {
            _logger.Information("[SPEED] Current migration pace: {pace:0.00} ad/s", _speedEstimatingCounter / 60d);
            _speedEstimatingCounter = 0;
        }

        private void DumpDeferredQueue()
        {
            if (!_deferredQueue.IsEmpty)
            {
                _logger.Fatal("[DEFERRED_QUEUE_DUMP] {queue}", string.Join(",", _deferredQueue.Select(m => m.Uuid)));
            }
        }
    }
}
