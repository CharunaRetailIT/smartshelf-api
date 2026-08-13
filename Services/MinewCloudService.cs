using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models.shelf;
using static TERMS_LOYALTY_API.DTOs.shelf.MiewProduct;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewTemplate;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewBinding;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewLogin;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewStore;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewDeviceBatchAdd;

namespace TERMS_LOYALTY_API.Services
{
    public class MinewCloudService
    {
        private readonly HttpClient _http;
        private string? _cachedToken;
        private readonly IOptions<MinewLoginSettings> _loginSettings;

        // The token belongs to one Minew account for the whole process, so it is
        // cached statically rather than per instance - MinewCloudService is
        // registered both via AddHttpClient (transient) and AddSingleton, and a
        // per-instance cache would silently stop working if that order changed.
        //
        // Without this every bind performed a fresh login; a run of ~25 binds was
        // enough for Minew to start rejecting the logins outright.
        private static string? _sharedToken;
        private static DateTime _sharedTokenExpiresUtc;
        private static readonly SemaphoreSlim _tokenGate = new SemaphoreSlim(1, 1);

        // Minew's login response carries no expiry, so refresh on a conservative
        // schedule and rely on InvalidateToken() when a call is rejected.
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

        public MinewCloudService(HttpClient httpClient, IOptions<MinewLoginSettings> loginSettings)
        {
            _http = httpClient;
            _http.BaseAddress = new Uri("https://cloud.minewesl.com/");
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _loginSettings = loginSettings;
        }

        // ==============================================
        // #region LOGIN
        // ==============================================
        #region Login
        public async Task<string?> LoginAsync(string username, string password)
        {
            string hashedPassword = password.ToMd5Lower();
            var body = new MinewLoginRequest
            {
                username = username,
                password = hashedPassword
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/action/login", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Login failed: {result}");

            var loginResponse = JsonSerializer.Deserialize<MinewLoginResponse>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

            if (loginResponse?.code == 200)
            {
                _cachedToken = loginResponse.data.token?.Trim();
                return _cachedToken;
            }

            throw new Exception($"Login error: {loginResponse?.msg}");
        }

        /// <summary>
        /// Returns a token for the configured Minew account, logging in only when
        /// there is no usable cached one. Prefer this over calling LoginAsync,
        /// which always performs a network round trip.
        /// </summary>
        public async Task<string> GetValidTokenAsync()
        {
            if (TryGetCachedToken(out var cached))
                return cached;

            await _tokenGate.WaitAsync();
            try
            {
                // Another caller may have refreshed while we waited on the gate.
                if (TryGetCachedToken(out cached))
                    return cached;

                var token = await LoginAsync(
                    _loginSettings.Value.Username,
                    _loginSettings.Value.Password);

                if (string.IsNullOrEmpty(token))
                    throw new Exception("Minew login returned an empty token");

                _sharedToken = token;
                _sharedTokenExpiresUtc = DateTime.UtcNow.Add(TokenLifetime);
                return token;
            }
            finally
            {
                _tokenGate.Release();
            }
        }

        /// <summary>
        /// Drops the cached token so the next call re-authenticates. Call this
        /// when Minew rejects a request that used the cached token.
        /// </summary>
        public static void InvalidateToken()
        {
            _sharedToken = null;
            _sharedTokenExpiresUtc = default;
        }

        private bool TryGetCachedToken(out string token)
        {
            var shared = _sharedToken;
            if (!string.IsNullOrEmpty(shared) && DateTime.UtcNow < _sharedTokenExpiresUtc)
            {
                // Keep the instance field in step - SetTokenHeaders reads it.
                _cachedToken = shared;
                token = shared;
                return true;
            }

            token = null;
            return false;
        }

        private async Task EnsureTokenAsync()
        {
            await GetValidTokenAsync();
        }

        private void SetTokenHeaders(string token = null)
        {
            // Remove any existing Authorization or token headers
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Remove("token");

            // Add the token as a header
            _http.DefaultRequestHeaders.TryAddWithoutValidation("token", token ?? _cachedToken);
        }

        #endregion

        // ==============================================
        // #region STORE HANDLING
        // ==============================================
        #region Store

        public async Task<MinewStoreResponse?> GetStoresAsync(string token, int active = 1, string? condition = null)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var url = $"apis/esl/store/list?active={active}";
            if (!string.IsNullOrWhiteSpace(condition))
                url += $"&condition={Uri.EscapeDataString(condition)}";

            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetStores failed: {result}");

            var stores = JsonSerializer.Deserialize<MinewStoreResponse>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return stores;
        }

        public async Task<string> AddStoreAsync(MinewAddStoreRequest store)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(store);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/store/add", content);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        public async Task<string> UpdateStoreAsync(MinewUpdateStoreRequest store)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(store);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync("apis/esl/store/update", content);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        public async Task<string> OpenOrCloseStoreAsync(string storeId, int active)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var url = $"apis/esl/store/openOrClose?storeId={storeId}&active={active}";
            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        #endregion

        // ==============================================
        // #region DEVICE HANDLING - NEW METHODS
        // ==============================================
        #region Device Management

        //public async Task<dynamic> GetDevicesFromCloud(string token, string storeId, string eqstatus = "2,8,9")
        //{
        //    await EnsureTokenAsync();
        //    SetTokenHeaders(token);

        //    var url = $"apis/esl/label/cascadQuery?page=1&size=100&storeId={storeId}&eqstatus={eqstatus}&type=1";
        //    var response = await _http.GetAsync(url);
        //    var result = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception($"Get devices failed: {result}");

        //    return JsonSerializer.Deserialize<dynamic>(result);
        //}

        public async Task<MinewDeviceResponse> GetDevicesFromCloud(string token,string storeId,string eqstatus = "2,8,9")
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/label/cascadQuery?page=1&size=100&storeId={storeId}&eqstatus={eqstatus}&type=1";
            var response = await _http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get devices failed: {json}");

            return JsonSerializer.Deserialize<MinewDeviceResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    // Minew quotes numbers inconsistently between fields and
                    // firmware versions; without this a single "100" instead of
                    // 100 fails the whole device list.
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                });
        }

        public async Task<dynamic> LightUpDevice(string token, string mac, string storeId, int color,
            int total, int period, int interval, int brightness)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/label/led?storeId={storeId}&color={color}&total={total}" +
                      $"&period={period}&interval={interval}&brightness={brightness}&mac={mac}";

            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Light up failed: {result}");

            // Minew answers some requests (an unknown MAC, for one) with HTTP 200
            // and an empty body. Deserialising that threw a raw parser error -
            // "The input does not contain any JSON tokens" - which surfaced as a
            // 500 and told the operator nothing.
            if (string.IsNullOrWhiteSpace(result))
                throw new Exception(
                    "Minew returned an empty response - the label may not exist in this store.");

            try
            {
                return JsonSerializer.Deserialize<dynamic>(result);
            }
            catch (JsonException)
            {
                throw new Exception(
                    $"Minew returned an unreadable response: " +
                    (result.Length <= 200 ? result : result.Substring(0, 200) + "..."));
            }
        }

        public async Task<dynamic> GetDeviceStatus(string token, string storeId, string mac)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/label/getByMac?storeId={storeId}&mac={mac}";
            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get device status failed: {result}");

            return JsonSerializer.Deserialize<dynamic>(result);
        }

        public async Task<MinewBatchAddResponse> BatchAddDevicesAsync( string token, string storeId,List<string> macAddresses, int type = 1)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var request = new MinewBatchAddRequest
            {
                StoreId = storeId,
                MacArray = macAddresses,
                Type = type
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/label/batchAdd", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Batch add devices failed: {result}");

            return JsonSerializer.Deserialize<MinewBatchAddResponse>(
                result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<MinewBatchWakeResponse> BatchWakeDevicesAsync(string token,string storeId, List<string> macAddresses)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/label/batchWake?storeId={Uri.EscapeDataString(storeId)}";

            var json = JsonSerializer.Serialize(macAddresses);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Batch wake devices failed: {result}");

            return JsonSerializer.Deserialize<MinewBatchWakeResponse>(
                result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }


        #endregion


        // ==============================================
        // #region GATEWAY HANDLING - NEW METHODS
        // ==============================================
        #region Gateway Management

        public async Task<MinewGatewayResponse> GetGatewaysFromCloud(string token, string storeId, int page = 1, int size = 100)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/gateway/listPage?page={page}&size={size}&storeId={storeId}";
            var response = await _http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get gateways failed: {json}");

            var result = JsonSerializer.Deserialize<MinewGatewayResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result;
        }

        public async Task<dynamic> AddGatewayToCloud(string token, string mac, string name, string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var request = new
            {
                mac = mac.Replace(":", "").Replace("-", "").Replace(" ", "").ToUpper(),
                name = name,
                storeId = storeId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/gateway/add", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Add gateway failed: {result}");

            return JsonSerializer.Deserialize<dynamic>(result);
        }

        public async Task<dynamic> DeleteGatewayFromCloud(string token, string gatewayId, string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/gateway/delete?id={gatewayId}&storeId={storeId}";
            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Delete gateway failed: {result}");

            return JsonSerializer.Deserialize<dynamic>(result);
        }

        public async Task<dynamic> UpdateGatewayInCloud(string token, string gatewayId, string name)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var request = new
            {
                id = gatewayId,
                name = name
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/gateway/update", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Update gateway failed: {result}");

            return JsonSerializer.Deserialize<dynamic>(result);
        }

        // Helper method for generic GET requests to Minew API
        //public async Task<T> GetFromMinewAsync<T>(string token, string url) where T : class
        //{
        //    await EnsureTokenAsync();
        //    SetTokenHeaders(token);

        //    var response = await _http.GetAsync(url);
        //    var json = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception($"GET request failed: {json}");

        //    return JsonSerializer.Deserialize<T>(
        //        json,
        //        new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        });
        //}

        /// <summary>
        /// Returns the merchant's dynamic field ids (apis/esl/scene/findDongTaiZiDuan).
        ///
        /// A goodsMap key must be the id of the field a template element is bound
        /// to - "image" is not a universal key, and Minew silently ignores any key
        /// it does not recognise, so a picture sent under the wrong one just never
        /// appears on the label.
        /// </summary>
        public async Task<List<MinewDynamicField>> GetDynamicFieldsAsync(string token)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var response = await _http.PostAsync(
                "apis/esl/scene/findDongTaiZiDuan",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
                throw new Exception($"Could not read Minew dynamic fields: {json}");

            var parsed = JsonSerializer.Deserialize<MinewDynamicFieldResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                });

            if (parsed == null || parsed.Code != 200)
                throw new Exception($"Minew dynamic fields failed: {parsed?.Msg ?? json}");

            return parsed.Data ?? new List<MinewDynamicField>();
        }

        public async Task<T> GetFromMinewAsync<T>(string token, string url) where T : class
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            using var response = await _http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Minew GET failed ({response.StatusCode}): {json}");

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<T>(json, options);

                if (result == null)
                    throw new Exception("Minew returned empty JSON.");

                return result;
            }
            catch (JsonException ex)
            {
                // This prevents silent 500s and shows the real cause
                throw new Exception(
                    $"Failed to deserialize Minew response to {typeof(T).Name}. Raw JSON: {json}",
                    ex
                );
            }
        }


        // Helper method for generic POST requests to Minew API
        public async Task<T> PostToMinewAsync<T>(string token, string url, object data) where T : class
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"POST request failed: {result}");

            return JsonSerializer.Deserialize<T>(
                result,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        #endregion

        // ==============================================
        // #region TEMPLATE HANDLING - NEW METHODS
        // ==============================================
        #region Template Management

        //public async Task<dynamic> GetTemplatesFromCloud(string token, string storeId)
        //{
        //    await EnsureTokenAsync();
        //    SetTokenHeaders(token);

        //    var url = $"apis/esl/template/findAll?page=1&size=50&storeId={storeId}&screening=2";
        //    var response = await _http.GetAsync(url);
        //    var result = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception($"Get templates failed: {result}");

        //    return JsonSerializer.Deserialize<dynamic>(result);
        //}

        public async Task<MinewTemplateResponse> GetTemplatesFromCloud(
    string token,
    string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/template/findAll?page=1&size=50&storeId={storeId}&screening=2";
            var response = await _http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get templates failed: {json}");

            return JsonSerializer.Deserialize<MinewTemplateResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }


        public async Task<string> GetUnboundTemplatePreview(string token, string templateId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/template/previewTemplate?demoName={Uri.EscapeDataString(templateId)}";
            var response = await _http.PostAsync(url, null);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get unbound preview failed: {result}");

            var responseObj = JsonSerializer.Deserialize<dynamic>(result);
            return responseObj?.GetProperty("data").GetString();
        }

        public async Task<string> GetBoundTemplatePreview(string token, string templateId, string mac, string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var url = $"apis/esl/template/preview?demoName={Uri.EscapeDataString(templateId)}&id={Uri.EscapeDataString(mac)}&storeId={storeId}";
            var response = await _http.PostAsync(url, null);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get bound preview failed: {result}");

            var responseObj = JsonSerializer.Deserialize<dynamic>(result);
            return responseObj?.GetProperty("data").GetString();
        }

        public async Task<string> GetTemplatePreviewWithData(string token, string templateId,
            Dictionary<string, string> data, string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            // For preview with data, we might need to use a different endpoint
            // Using bound preview with test data for now
            var url = $"apis/esl/template/preview?demoName={Uri.EscapeDataString(templateId)}&id=test_data&storeId={storeId}";

            // If we need to send custom data, we might need to use a POST request
            if (data != null && data.Count > 0)
            {
                var requestBody = new { previewing = data };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Get preview with data failed: {result}");

                var responseObj = JsonSerializer.Deserialize<dynamic>(result);
                return responseObj?.GetProperty("data").GetString();
            }
            else
            {
                var response = await _http.PostAsync(url, null);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Get preview with data failed: {result}");

                var responseObj = JsonSerializer.Deserialize<dynamic>(result);
                return responseObj?.GetProperty("data").GetString();
            }
        }

        #endregion

        // ==============================================
        // #region BIND DATA - NEW METHODS
        // ==============================================
        #region Bind Data

        //public async Task<dynamic> BindData(string token, object bindRequest)
        //{
        //    await EnsureTokenAsync();
        //    SetTokenHeaders(token);

        //    var json = JsonSerializer.Serialize(bindRequest);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await _http.PostAsync("apis/esl/label/updateBindBrush", content);
        //    var result = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception($"Bind data failed: {result}");

        //    return JsonSerializer.Deserialize<dynamic>(result);
        //}

        public async Task<JsonDocument> BindData(string token, object bindRequest)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var json = JsonSerializer.Serialize(bindRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/label/updateBindBrush", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bind data failed: {result}");

            return JsonDocument.Parse(result);
        }

        public async Task<dynamic> BatchBindData(string token, List<object> bindRequests)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            var json = JsonSerializer.Serialize(new { bindList = bindRequests });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/label/batchUpdateBindBrush", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Batch bind data failed: {result}");

            return JsonSerializer.Deserialize<dynamic>(result);
        }

        // Deletes the binding relationship between a tag and its data/template.
        // If the tag has an initialization template, it reverts to that; otherwise it goes blank.
        public async Task<MinewUnbindResponse> UnbindDeviceAsync(string mac, string storeId)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var url = $"apis/esl/label/deleteBind?mac={Uri.EscapeDataString(mac)}&storeId={Uri.EscapeDataString(storeId)}";

            var response = await _http.PostAsync(url, null);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Unbind device failed: {result}");

            return JsonSerializer.Deserialize<MinewUnbindResponse>(
                result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        #endregion

        // ==============================================
        // #region PRODUCT HANDLING
        // ==============================================
        #region Product

        public async Task<string> GetProductsAsync(string storeId, int page = 1, int size = 10, string? condition = null)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var url = $"apis/esl/goods/getByStoreId?storeId={storeId}&page={page}&size={size}";
            if (!string.IsNullOrWhiteSpace(condition)) url += $"&condition={Uri.EscapeDataString(condition)}";

            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"GetProducts failed: {result}");
            return result;
        }

        public async Task<MinewUpdateProductBatchResponse?> UpdateProductsInBatchAsync(MinewUpdateProductBatchRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/goods/update", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Batch update failed: {result}");

            var updateResponse = JsonSerializer.Deserialize<MinewUpdateProductBatchResponse>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return updateResponse;
        }

        // Update product with bind
        public async Task<MinewUpdateProductResponse?> UpdateProductInStoreAsync(MinewUpdateProductRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/goods/updateToStore", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Update product in store failed: {result}");

            var updateResponse = JsonSerializer.Deserialize<MinewUpdateProductResponse>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return updateResponse;
        }

        public async Task<MinewDeleteProductBatchResponse?> DeleteProductsInBatchAsync(MinewDeleteProductBatchRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/goods/batchDelete", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Batch delete failed: {result}");

            var deleteResponse = JsonSerializer.Deserialize<MinewDeleteProductBatchResponse>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return deleteResponse;
        }

        public async Task<string> AddProductToStoreAsync(MinewAddProductRequest product)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(product);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/goods/addToStore", content);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        #endregion

        // ==============================================
        // #region TEMPLATE MANAGEMENT (Existing)
        // ==============================================
        #region Template Management - Existing

        public async Task<string> GetTemplateListAsync(MinewTemplateListRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var url = $"apis/esl/template/list?storeId={request.storeId}&page={request.page}&size={request.size}";
            if (!string.IsNullOrWhiteSpace(request.condition))
                url += $"&condition={Uri.EscapeDataString(request.condition)}";

            var response = await _http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetTemplateList failed: {result}");

            return result;
        }

        public async Task<string> PreviewTemplateUnboundAsync(MinewTemplatePreviewUnboundRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/template/previewUnbound", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PreviewTemplateUnbound failed: {result}");

            return result;
        }

        public async Task<string> PreviewTemplateBoundAsync(MinewTemplatePreviewBoundRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/template/previewBound", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PreviewTemplateBound failed: {result}");

            return result;
        }

        #endregion

        // ==============================================
        // #region TEMPLATE BINDING (Existing)
        // ==============================================
        #region Template Binding - Existing

        public async Task<string> BatchBindGoodsToTemplateAsync(MinewBatchBindWithTemplateRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/device/batchBind", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"BatchBindGoodsToTemplate failed: {result}");

            return result;
        }

        public async Task<string> BatchBindAndUpdateWithTemplateAsync(MinewBatchBindAndUpdateWithTemplateRequest request)
        {
            await EnsureTokenAsync();
            SetTokenHeaders();

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("apis/esl/device/batchBindAndUpdate", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"BatchBindAndUpdateWithTemplate failed: {result}");

            return result;
        }

        #endregion

        // ==============================================
        // #region UTILITY METHODS
        // ==============================================
        #region Utility Methods

        public async Task<dynamic> TestConnection(string token)
        {
            await EnsureTokenAsync();
            SetTokenHeaders(token);

            try
            {
                // Try to get stores as a connection test
                var url = "apis/esl/store/list?active=1&size=1";
                var response = await _http.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Connection test failed: {result}");

                return JsonSerializer.Deserialize<dynamic>(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection test error: {ex.Message}");
            }
        }

        public async Task<string> RefreshToken()
        {
            // Clear cached token and get a new one
            _cachedToken = null;
            await EnsureTokenAsync();
            return _cachedToken;
        }

        #endregion
    }

    // ==============================================
    // #region EXTENSION METHODS
    // ==============================================
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to a 32-bit lowercase MD5 hash.
        /// </summary>
        public static string ToMd5Lower(this string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }

    // ==============================================
    // #region ADDITIONAL DTOs NEEDED
    // ==============================================
    public class MinewTemplateResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public MinewTemplateData Data { get; set; }
    }

    public class MinewTemplateData
    {
        public List<MinewTemplateRow> Rows { get; set; }
    }

    public class MinewTemplateRow
    {
        public string DemoId { get; set; }
        public string DemoName { get; set; }
        public string Color { get; set; }
        public int Orientation { get; set; }
        public string StoreId { get; set; }
        public ScreenSize ScreenSize { get; set; }
    }

    public class ScreenSize
    {
        public decimal Inch { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }


    public class MinewDynamicFieldResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public List<MinewDynamicField> Data { get; set; }
    }

    /// <summary>One user-defined goodsMap field as Minew defines it.</summary>
    public class MinewDynamicField
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int? Number { get; set; }
        public int? ColunmDataType { get; set; }
    }

    public class MinewDeviceResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public List<MinewDeviceItem> Items { get; set; }
    }

    public class MinewDeviceItem
    {
        public string Id { get; set; }
        public string Mac { get; set; }
        public string remark { get; set; }
        public string ScreenSize { get; set; }
        // Minew has been seen returning this as a quoted string (and it can
        // be absent entirely), which broke deserialisation of the whole
        // device list with "The JSON value could not be converted to
        // System.Int32". Nullable + AllowReadingFromString accepts 100,
        // "100" and null alike. Every consumer stores it in an int? anyway.
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Battery { get; set; }
        public string Firmware { get; set; }
        public string Hardware { get; set; }
        public string IsOnline { get; set; }
        public string StoreId { get; set; }

        public string Lastupdate { get; set; }
        public ScreenInfo ScreenInfo { get; set; }

    }
    public class ScreenInfo
    {
        public decimal? Inch { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Color { get; set; }
    }

    public class DeviceSyncRequest
    {
        public string StoreId { get; set; }
        public string EqStatus { get; set; } = "2,8,9";
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 100;
    }

    public class TemplateSyncRequest
    {
        public string StoreId { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 50;
        public int Screening { get; set; } = 2; // 0: all, 1: system, 2: store
    }

    public class BindDataRequestDto
    {
        public string StoreId { get; set; }
        public string LabelMac { get; set; }
        public Dictionary<string, string> GoodsMap { get; set; }
        public Dictionary<string, string> DemoIdMap { get; set; }
        public int? Color { get; set; }
        public int? Total { get; set; }
        public int? Period { get; set; }
        public int? Interval { get; set; }
        public int? Brightness { get; set; }
        public int? OpCode { get; set; }
    }

    public class LightUpRequestDto
    {
        public string StoreId { get; set; }
        public string Mac { get; set; }
        public int Color { get; set; } = 1;
        public int Total { get; set; } = 5;
        public int Period { get; set; } = 500;
        public int Interval { get; set; } = 900;
        public int Brightness { get; set; } = 100;
    }
}