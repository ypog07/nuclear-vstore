using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using AmsMigrator.DTO.Okapi;
using AmsMigrator.Exceptions;
using AmsMigrator.Models;

using Newtonsoft.Json;

using Polly;
using Polly.Retry;

using Serilog;

namespace AmsMigrator.Infrastructure
{
    public class OkapiClient : IOkapiClient
    {
        string _baseUri;
        string _apiVersion;
        private readonly RetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILogger _logger = Log.Logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _moderationStateMap;

        readonly HttpStatusCode[] _httpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.Gone, // 410
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout, // 504
            (HttpStatusCode)423 // Locked
        };

        public OkapiClient(ImportOptions options)
        {
            _httpClient = new HttpClient();
            _httpClient.Configure(token: options.OkApiAuthToken, timeout: 30);
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;

            _baseUri = options.OkApiBaseUri;
            _apiVersion = options.OkApiApiVersion;

            _retryPolicy = Policy
              .Handle<HttpRequestException>()
              .Or<TaskCanceledException>()
              .OrResult<HttpResponseMessage>(r => _httpStatusCodesWorthRetrying.Contains(r.StatusCode))
              .WaitAndRetryAsync(options.HttpServicesRetryCount, retryAttempt => TimeSpan.FromSeconds(10), (dr, ts, rc, _) =>
              {
                  if (dr.Exception != null)
                      _logger.Error("Exception {0} occured, retry attempt: {1}, timeout: {2}", dr.Exception.GetType().Name, rc, ts);
                  if (dr.Result != null)
                      _logger.Error("Invalid result {0} obtained, retry attempt: {1}, timeout: {2}", dr.Result, rc, ts);
              });

            _moderationStateMap = new Dictionary<string, string>
            {
                { "ready", "approved" }
            };
        }

        public async Task<MaterialStub> CreateMaterialStubAsync(string type, string code, long firm, string language)
        {
            var uri = _baseUri + $"/api/{_apiVersion}/{type}/{code}/session?firm={firm}&languages={language}";

            try
            {
                using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(uri, new StringContent(String.Empty, Encoding.UTF8, "application/json"))))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Error("Unsuccessfull response with code {code} and content {content}", response.StatusCode, content);
                        response.EnsureSuccessStatusCode();
                    }

                    return content != null ? MaterialStub.FromJson(content).First() : null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occured while creating AM template");
                throw;
            }
        }

        public async Task<MaterialStub> CreateNewMaterialAsync(long id, long firm, MaterialStub stub)
        {
            var uri = _baseUri + $"/api/{_apiVersion}/am/{id}?firm={firm}";

            try
            {
                var reqContent = stub.ToJson();
                using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PutAsync(uri, new StringContent(reqContent, Encoding.UTF8, "application/json"))))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == (HttpStatusCode)422)
                        {
                            _logger.Warning("[CREATE_MATERIAL] Advertisement material with id {advertisementId} does not satisfy some requirements and won't be imported. Errors: Response: {response}; Content: {content}", id, response, content);
                            throw new UnprocessableEntityException($"Unprocessable entity ({response.StatusCode}) request status received.", content: content);
                        }

                        if (response.StatusCode == HttpStatusCode.Conflict)
                        {
                            _logger.Warning("Conflict occured while creating material with id {id}; Response: {response}; Content: {content}", id, response, content);
                            _logger.Information("[RECOVERING] Trying to get material from okapi...", id, response, content);

                            var am = await GetMaterialAsync(id);
                            return am.FirstOrDefault();
                        }

                        _logger.Error("Unsuccessfull response with code {code} and content {content}", response.StatusCode, content);
                        response.EnsureSuccessStatusCode();
                    }

                    return content != null ? MaterialStub.SingleFromJson(content) : null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occured while creating new AM");
                throw;
            }
        }

        private async Task<MaterialStub[]> GetMaterialAsync(long id)
        {
            var uri = _baseUri + $"/api/{_apiVersion}/am/{id}";

            using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(uri)))
            {
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Unsuccessfull response with code {code} and content {content}; Material id: {id}", response.StatusCode, content, id);
                    response.EnsureSuccessStatusCode();
                }

                return content != null ? MaterialStub.FromJson(content) : null;
            }

        }

        public async Task<bool> SetModerationState(long amId, string version, Amsv1MaterialData materialData)
        {
            var uri = _baseUri + $"/api/{_apiVersion}/am/{amId}/version/{version}/moderation";

            try
            {
                var status = materialData.ModerationState;
                if (_moderationStateMap.TryGetValue(materialData.ModerationState, out var mappedStatus))
                {
                    status = mappedStatus;
                }

                var request = new ModerationRequest { Status = status, Comment = materialData.ModerationComment ?? string.Empty };
                var reqContent = new StringContent(request.ToJson(), Encoding.UTF8, "application/json");

                using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PutAsync(uri, reqContent)))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            _logger.Warning("[MODERATION_FAIL] Unable to add moderation to material {amid} uuid {uuid} firm {firmid}. Moderation info: {info}, Response: {content}", amId, materialData.Uuid, materialData.FirmId, request, content);
                        }
                        else
                        {
                            response.EnsureSuccessStatusCode();
                        }
                    }

                    _logger.Information("[MODERATION_SUCCESS] Ad material {uuid} {firmid} successfully moderated with state: {state}, comment: {comment}", materialData.Uuid, materialData.FirmId, materialData.ModerationState, materialData.ModerationComment);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occured while moderating material {amid}", amId);
                throw;
            }
        }

        public async Task<UploadResponse> UploadFileAsync(long advertisementId, Uri uploadUrl, string fileName, byte[] fileData, string imageSizeHeaderValue = null)
        {
            var url = uploadUrl;
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be empty");
            }

            try
            {
                using (var content = new MultipartFormDataContent())
                using (var memoryStream = new MemoryStream(fileData, false))
                using (var streamContent = new StreamContent(memoryStream))
                {
                    content.Add(streamContent, fileName, fileName);

                    if (!string.IsNullOrEmpty(imageSizeHeaderValue))
                    {
                        content.Headers.Add("x-ams-file-type", "sizeSpecificBitmapImage");
                        content.Headers.Add("x-ams-image-size", imageSizeHeaderValue);
                    }

                    using (var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(url, content)))
                    {
                        var stringResponse = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode == (HttpStatusCode)422 || response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                            {
                                _logger.Warning("[UPLOAD_IMAGE] Advertisement material with id {advertisementId} does not satisfy some requirements and won't be imported. Response: {response}; Content: {stringResponse}", advertisementId, response, stringResponse);
                                throw new UnprocessableEntityException($"Unprocessable entity ({response.StatusCode}) request status received.", customHeader: imageSizeHeaderValue, content: stringResponse);
                            }
                            _logger.Error("[UPLOAD_FAIL] Unsuccessfull response {response} with content {content} occured while uploading file: {fileName}, material id {materialid}", response, stringResponse, fileName, advertisementId);
                            response.EnsureSuccessStatusCode();
                        }

                        return JsonConvert.DeserializeObject<UploadResponse>(stringResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occured while uploading file by url: {url}{size}", uploadUrl, string.IsNullOrWhiteSpace(imageSizeHeaderValue) ? "" : $", size: {imageSizeHeaderValue}");
                throw;
            }
        }
    }
}
