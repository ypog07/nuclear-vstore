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

namespace AmsMigrator
{
    public class AdvertisementMaterialsImporter
    {
        private readonly IErmDbClient _dbClient;
        private readonly ImportOptions _options;
        private readonly IAmsClient _amsClient;
        private readonly IServiceProvider _container;
        private readonly ILogger _logger = Log.Logger;
        private readonly HashSet<long> _kbFirms;
        private readonly HashSet<long> _zmkFirms;
        private readonly ConcurrentQueue<Amsv1MaterialData> _deferredQueue;
        private int _processedAdCounter;
        private int _processedLogoCounter;
        private int _processedKbCounter;
        private int _processedZmkCounter;
        private int _bindedOrdersCounter;
        private int _fetchedAmCounter;

        public AdvertisementMaterialsImporter(ImportOptions options, IAmsClient amsClient, IErmDbClient dbClient, IServiceProvider container)
        {
            _amsClient = amsClient;
            _options = options;
            _dbClient = dbClient;
            _container = container;
            _kbFirms = _dbClient.GetFirmIdsForGivenPositions(_options.ThresholdDate, _options.KbLogoTriggerNomenclatures).ToHashSet();
            _zmkFirms = _dbClient.GetFirmIdsForGivenPositions(_options.ThresholdDate, _options.ZmkLogoTriggerNomenclatures).ToHashSet();
            _deferredQueue = new ConcurrentQueue<Amsv1MaterialData>();

            Stats.Collector["fetch"] = 0;
            Stats.Collector["import"] = 0;
            Stats.Collector["imgProc"] = 0;
            Stats.Collector["binding"] = 0;
            Stats.Collector["full"] = 0;
        }

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
                            new ParallelOptions { MaxDegreeOfParallelism = 4 }, PatchSourceImportData);

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

                firms = firms.OrderBy(n => n).ToList();

                if (_options.StartAfterFirmId != null)
                {
                    firms = firms.SkipWhile(firmId => firmId <= startAfterFirmId).ToList();
                }

                if (_options.TestMode.Enabled)
                {
                    firms = firms.Take(testModeLimit).ToList();
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

                        PatchSourceImportData(amsv1Data);

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
                    var logoFirms = firms.Except(_kbFirms).Except(_zmkFirms).ToList();
                    var kbFirms = firms.Intersect(_kbFirms).ToList();
                    var zmkFirms = firms.Intersect(_zmkFirms).ToList();

                    await RunBatchedImport(logoFirms, false);
                    await RunBatchedImport(kbFirms, false);
                    await RunBatchedImport(zmkFirms, true);
                }

                _logger.Information("Executing retry queue. Initial queue length {length}", _deferredQueue.Count);

                while (_deferredQueue.TryDequeue(out var ad))
                {
                    var bindingData = await PerformImportForSingleData(ad);
                    await RunBindingWorker(bindingData);

                    _logger.Information("{length} elements remaining in queue.", _deferredQueue.Count);
                }

                sw.Stop();

                DumpDeferredQueue();

                return _processedAdCounter;
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

                if (_options.TestMode.Enabled)
                {
                    _logger.Information("Test mode execution is finished. Test data size is: {size}", _options.TestMode.Limit);
                }
            }
        }

        private async Task RunBatchedImport(IEnumerable<long> firmIds, bool fetchMiniImages)
        {
            var batches = firmIds
                             .Select((x, i) => new { Index = i, Value = x })
                             .GroupBy(x => x.Index / _options.BatchSize)
                             .Select(x => x.Select(v => v.Value).ToArray());

            foreach (var batch in batches)
            {
                var amsv1Data = await RunFetchingWorker(batch, fetchMiniImages);
                var bindingData = await RunImportWorker(amsv1Data);
                await RunBindingWorker(bindingData);
            }
        }

        private async Task<Amsv1MaterialData[]> RunFetchingWorker(long[] part, bool miniImagesNeeded)
        {
            var sw = new Stopwatch();

            sw.Start();
            var amsv1Data = await _amsClient.GetAmsv1DataAsync(part, miniImagesNeeded);
            sw.Stop();
            Stats.Collector["fetch"] += sw.ElapsedMilliseconds;
            sw.Reset();
            Interlocked.Add(ref _fetchedAmCounter, amsv1Data.Length);

            sw.Start();
            Parallel.ForEach(amsv1Data,
                new ParallelOptions { MaxDegreeOfParallelism = 4 }, PatchSourceImportData);
            sw.Stop();
            Stats.Collector["imgProc"] += sw.ElapsedMilliseconds;
            sw.Reset();

            return amsv1Data;
        }

        private async Task<List<OrderMaterialBindingData>> RunImportWorker(Amsv1MaterialData[] amsv1Data)
        {
            var sw = new Stopwatch();

            var orderBindingDataList = new List<OrderMaterialBindingData>();

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

        private async Task RunBindingWorker(List<OrderMaterialBindingData> orderBindingDataList)
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

        private void PatchSourceImportData(Amsv1MaterialData amsv1Data)
        {
            var (data, ext) = ImageProcessor.FillImageBackground(amsv1Data.ImageDataOriginal, amsv1Data.BackgroundColor);
            amsv1Data.ImageData = data;
            amsv1Data.ImageExt = ext ?? amsv1Data.ImageExt;
        }

        private async Task<List<OrderMaterialBindingData>> PerformImportForSingleData(Amsv1MaterialData data)
        {
            try
            {
                var bindingDataList = new List<OrderMaterialBindingData>();
                _logger.Information("Starting to import AMS 1.0 material with uuid: {uuid}", data.Uuid);
                _logger.Information("Processing logo image: {image}", data);

                if (_options.Targets.HasFlag(ImportTarget.Logotypes) && !data.ReachedTarget.HasFlag(ImportTarget.Logotypes))
                {
                    var logoImportStrategy = _container.GetService<CompanyLogoImportStrategy>();

                    var bindingData = await logoImportStrategy.ExecuteAsync(data);
                    if (bindingData != null)
                    {
                        bindingDataList.Add(bindingData);

                        data.Complete(ImportTarget.Logotypes);

                        Interlocked.Increment(ref _processedLogoCounter);
                    }
                }

                if (_options.Targets.HasFlag(ImportTarget.ZmkBrending) &&
                    data.HasSizeSpecificImages &&
                    _zmkFirms.Contains(data.FirmId) &&
                    !data.ReachedTarget.HasFlag(ImportTarget.ZmkBrending))
                {
                    var zmkStrategy = _container.GetService<ZmkLogoImportStrategy>();

                    var bindingData = await zmkStrategy.ExecuteAsync(data);
                    if (bindingData != null)
                    {
                        bindingDataList.Add(bindingData);

                        data.Complete(ImportTarget.ZmkBrending);

                        Interlocked.Increment(ref _processedZmkCounter);
                    }
                }

                if (_options.Targets.HasFlag(ImportTarget.KbLogotypes) && _kbFirms.Contains(data.FirmId) && !data.ReachedTarget.HasFlag(ImportTarget.KbLogotypes))
                {
                    var kbLogoStrategy = _container.GetService<KBLogoImportStrategy>();

                    var bindingData = await kbLogoStrategy.ExecuteAsync(data);
                    if (bindingData != null)
                    {
                        bindingDataList.Add(bindingData);

                        data.Complete(ImportTarget.KbLogotypes);

                        Interlocked.Increment(ref _processedKbCounter);
                    }
                }

                Interlocked.Increment(ref _processedAdCounter);

                _logger.Information("[IMPORT_SUCCESS] AMS 1.0 material with uuid: {uuid} {firmid} has been imported successfully", data.Uuid, data.FirmId);

                return bindingDataList;
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
                    _logger.Warning("[SKIP_AD_IMPORT] Skip to process advertisement {uuid} {firmid} due to irresistible problem.", data.Uuid, data.FirmId);
                }
                return new List<OrderMaterialBindingData>();
            }
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
