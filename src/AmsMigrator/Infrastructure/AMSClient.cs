using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using AmsMigrator.DTO.AMS1;
using AmsMigrator.Models;

using Newtonsoft.Json.Linq;

using Polly;
using Polly.Retry;

using Serilog;

namespace AmsMigrator.Infrastructure
{
    public class AmsClient : IAmsClient
    {
        private readonly int[] _zmkSizes;
        private readonly string[] _statusesForMigration;
        private string _amsBaseUri;
        private readonly RetryPolicy<HttpResponseMessage> _retryPolicy;
        private ILogger _logger = Log.Logger;
        private HttpClient _httpClient;
        private ImportOptions _options;
        private Regex _colorCodePattern = new Regex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", RegexOptions.Compiled);
        private const string DefaultBackgroundColor = "#000000";

        HttpStatusCode[] _httpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.Gone, // 410
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        public AmsClient(ImportOptions options)
        {
            _httpClient = new HttpClient();
            _httpClient.Configure(token: options.Amsv1AuthToken, timeout: 30);

            _options = options;

            _zmkSizes = options.SizeSpecificImageSizes;
            _statusesForMigration = options.StatusesForMigration.Select(ms => ms.Name).ToArray();
            _amsBaseUri = options.Amsv1BaseUri;

            _retryPolicy = Policy
              .Handle<HttpRequestException>()
              .Or<TaskCanceledException>()
              .OrResult<HttpResponseMessage>(r => _httpStatusCodesWorthRetrying.Contains(r.StatusCode))
              .WaitAndRetryAsync(options.HttpServicesRetryCount, retryAttempt => TimeSpan.FromSeconds(10), (dr, ts, rc, _) =>
              {
                  if (dr.Exception != null)
                      _logger.Error(dr.Exception, "[HTTP_SRC] Exception {exception} occured, retry attempt: {retryCount}, timeout: {timeout}", dr.Exception.GetType().Name, rc, ts);
                  if (dr.Result != null)
                      _logger.Error("[HTTP_SRC] Invalid result {httpResult} obtained, retry attempt: {retryCount}, timeout: {timeout}", dr.Result, rc, ts);
              });
        }

        public async Task<Amsv1MaterialData> GetAmsv1DataAsync(long firmId, bool miniImagesNeeded)
        {
            _logger.Information("Getting uuid for firm id: {firmId}", firmId);
            var uuid = await GetUuidByFirmIdAsync(firmId);

            if (string.IsNullOrEmpty(uuid.Uuid))
                return null;

            _logger.Information("Getting logo data for uuid: {uuid}", uuid);
            var logoData = await GetLogoInfoByUidAsync(uuid, miniImagesNeeded);

            return logoData;
        }

        public async Task<Amsv1MaterialData[]> GetAmsv1DataAsync(long[] firmIds, bool miniImagesNeeded)
        {
            var uuids = await GetUuidsByFirmIdAsync(firmIds);

            if (!uuids?.Any() ?? true)
                return Enumerable.Empty<Amsv1MaterialData>().ToArray();

            _logger.Information($"Getting logo data for uuids: {string.Join(',', uuids.Select(u => u.Uuid))}");
            var data = await GetLogoInfoByUidAsync(uuids, miniImagesNeeded);

            return data;
        }

        private async Task<(string Uuid, string CommitHash)> GetUuidByFirmIdAsync(long firmid)
        {
            var uri = _amsBaseUri + $"/moderation?firm_id={firmid}&state={PrepareUrlParameterValue(_statusesForMigration)}";
            using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(uri)))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null, null);
                }
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = Amsv1Response.FromJson(json);

                var item = result.Result.Items.First();

                if (!_statusesForMigration.Contains(item.State))
                {
                    _logger.Warning("[UNACCEPTABLE_STATUS] AM with id {uuid} wouldn't be processed due unacceptable moderation status {state}.", item.Uuid, item.State);
                    return (null, null);
                }

                return (item.Uuid, item.Commit);
            }
        }

        private async Task<(string Uuid, string CommitHash)[]> GetUuidsByFirmIdAsync(long[] firmids)
        {
            var moderationUri = _amsBaseUri + $"/moderation?limit={_options.BatchSize}&firm_id={PrepareUrlParameterValue(firmids)}&state={PrepareUrlParameterValue(_statusesForMigration)}";

            using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(moderationUri)))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Warning("[EMPTY_FIRM] Am data not found for firms: {firms}", string.Join(",", firmids));
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var result = Amsv1Response.FromJson(json);

                var foundMaterialFirmIds = result.Result.Items.Select(i => long.Parse(i.FirmId));
                _logger.Warning("[EMPTY_FIRM] Am data not found for firms: {firms}", string.Join(",", firmids.Except(foundMaterialFirmIds)));

                foreach (var item in result.Result.Items)
                {
                    if (!_statusesForMigration.Contains(item.State))
                    {
                        _logger.Warning("[UNACCEPTABLE_STATUS] AM with id {uuid} wouldn't be processed due unacceptable moderation status {state}.", item.Uuid, item.State);
                    }
                }

                var uuids = result.Result.Items
                    .Where(i => _statusesForMigration.Contains(i.State))
                    .Select(i => (i.Uuid, i.Commit))
                    .ToArray();

                return uuids;
            }
        }

        private string PrepareUrlParameterValue<T>(T[] args)
        {
            var joinedArgs = string.Join(",", args);
            return WebUtility.UrlEncode(joinedArgs);
        }


        public async Task<Amsv1MaterialData[]> GetLogoInfoByUidAsync((string uuid, string hash)[] uuids, bool miniImagesNeeded)
        {
            var data = await Task.WhenAll(uuids.Select(u => GetLogoInfoByUidAsync(u, miniImagesNeeded)));

            return data.Where(d => d != null).ToArray();
        }

        private async Task<Amsv1MaterialData> GetLogoInfoByUidAsync((string uuid, string commitHash) input, bool miniImagesNeeded)
        {
            var uri = _amsBaseUri + $"/{input.uuid}/langs/ru?commit={input.commitHash}&locale={_options.Language}";

            using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(uri)))
            {
                if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.Error("[ENV_SYNC] AM with id {uuid} wouldn't be processed due to {status} server response.", input.uuid, response.StatusCode);
                    return null;
                }
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var materialData = LogoInfo.FromJson(json);

                var state = materialData.Commit.State;

                if (state.Equals("uploaded"))
                {
                    _logger.Information("[UPLOAD_FALLBACK] Trying to fallback ad {uuid} with uploaded state. Getting the latest actual version.", input.uuid);

                    var actualCommit = await GetActualCommitHash(input.uuid);
                    if (actualCommit.State == null)
                    {
                        _logger.Information("[UPLOAD_SKIP] Unable to fallback ad {uuid} with uploaded state.", input.uuid);
                        return null;
                    }

                    var actualData = await GetLogoInfoByUidAsync((input.uuid, actualCommit.Hash), miniImagesNeeded);

                    _logger.Information("[UPLOAD_FALLBACK_SUCCESS] Latest actual material {am} state {state} with commit hash {hash} was obtained.", input.uuid, actualCommit.State, actualCommit.Hash);

                    return actualData;
                }

                if (!_statusesForMigration.Contains(state))
                {
                    _logger.Warning("[UNACCEPTABLE_STATUS2] AM with id {uuid} wouldn't be processed due unacceptable moderation status {state}.", input.uuid, state);
                    return null;
                }

                var logo = materialData.GetFirstDataItem("logo");
                if (logo == null)
                {
                    _logger.Error("[NO_LOGO] The material {uuid} has no logo.", input.uuid);
                    return null;
                }

                var color = (string)materialData.GetFirstDataItem("bg_color")?.Content;
                var crop = ((JObject)materialData.GetFirstDataItem("crop").Content).ToObject<CropArea>();

                if (_options.MaxImageSizeLimit != null && logo.Height > _options.MaxImageSizeLimit || logo.Width > _options.MaxImageSizeLimit)
                {
                    _logger.Error("[IMG_TOO_LARGE] Logo image from am {uuid} firm {firm} cannot be imported, because its size exceeds the limit. Image size: {w}x{h}; Limit: {limit}x{limit}",
                        input.uuid, materialData.Meta.FirmId, logo.Width, logo.Height, _options.MaxImageSizeLimit);
                    return null;
                }

                var zmkData = materialData.GetDataItems(i => _zmkSizes.Any(s => i.Name.Equals($"image_{s}x{s}")));
                var hasModeration = materialData.Commit.Moderation == null;
                var comment = materialData.Commit.Moderation?.Overall;
                var firmid = materialData.Meta.FirmId;

                var data = new Amsv1MaterialData
                {
                    Uuid = input.uuid,
                    FirmId = long.Parse(firmid),
                    State = state,
                    BackgroundColor = FilterColorCode(color),
                    CropHeight = crop.Height,
                    CropWidth = crop.Width,
                    CropLeft = crop.Left,
                    CropTop = crop.Top,
                    ImageExt = logo.Ext,
                    ImageHeight = logo.Height,
                    ImageName = logo.Name,
                    ImageSize = logo.Size,
                    ImageType = logo.Type,
                    ImageUrl = logo.Url,
                    ImageWidth = logo.Width,
                    HasModerationInfo = hasModeration,
                    ModerationComment = comment,
                    ModerationState = state,
                    ReachedTarget = ImportTarget.None,
                    QueueTries = _options.QueueTriesCount
                };

                var imgContent = await GetFileContentAsync(logo.Url);
                data.ImageData = imgContent;

                if (zmkData.Any() && _options.Targets.HasFlag(ImportTarget.ZmkBrendingLogotypes) && miniImagesNeeded)
                {
                    data.SizeSpecificImages = await FetchCustomImagesDataAsync(zmkData);
                }

                return data;
            }

        }

        private string FilterColorCode(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return DefaultBackgroundColor;
            }

            return !_colorCodePattern.IsMatch(color) ? DefaultBackgroundColor : color.ToUpper();
        }

        private async Task<(string State, string Hash)> GetActualCommitHash(string uuid)
        {
            var uri = _amsBaseUri + $"/states/{uuid}";
            try
            {
                using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(uri)))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var states = States.FromJson(json);

                    if (states.Ru.TryGetValue("ready", out var actualCommitInfo))
                    {
                        return ("ready", actualCommitInfo);
                    }

                    if (states.Ru.TryGetValue("rejected", out actualCommitInfo))
                    {
                        return ("rejected", actualCommitInfo);
                    }

                    throw new InvalidOperationException("Actual commit not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while requesting actual AM commit hash.");
                return (null, null);
            }
        }

        private async Task<SizeSpecificImageData[]> FetchCustomImagesDataAsync(IEnumerable<Datum> zmkData)
        {

            try
            {
                var resultImages = new List<SizeSpecificImageData>();

                foreach (var data in zmkData)
                {
                    var single = await FetchCustomImageDataAsync(data);
                    resultImages.Add(single);
                }

                return resultImages.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while downloading custom logotypes");
                throw;
            }
        }

        private async Task<SizeSpecificImageData> FetchCustomImageDataAsync(Datum data)
        {
            var image = new SizeSpecificImageData
            {
                Name = $"{data.Name}.{data.Ext}",
                Height = data.Height,
                Width = data.Width,
                Url = data.Url
            };

            var content = await GetFileContentAsync(image.Url);

            image.Data = content;

            return image;
        }

        private async Task<byte[]> GetFileContentAsync(string fileUrl)
        {
            try
            {
                var uri = _amsBaseUri + fileUrl;
                using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(uri)))
                {
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsByteArrayAsync();
                    return content;
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"An error occurred while downloading file {fileUrl} content.", fileUrl);
                throw;
            }
        }
    }
}
