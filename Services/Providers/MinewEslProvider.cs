using System;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Services;

namespace TERMS_LOYALTY_API.Services.Providers
{
    // Thin adapter around MinewCloudService - no behavior change from the
    // previous direct-injection calls in DeviceController, just moved behind
    // IEslProvider so DeviceController no longer depends on Minew concretely.
    public class MinewEslProvider : IEslProvider
    {
        private readonly MinewCloudService _minewService;

        public MinewEslProvider(MinewCloudService minewService)
        {
            _minewService = minewService;
        }

        public string BrandCode => "Minew";

        private async Task<string> GetTokenAsync()
        {
            try
            {
                // Same hardcoded credentials previously inlined in DeviceController.GetToken().
                return await _minewService.GetValidTokenAsync();
            }
            catch (Exception)
            {
                throw new Exception("Failed to get authentication token. Please check login credentials.");
            }
        }

        public async Task<MinewDeviceResponse> GetDevicesFromCloudAsync(string cloudStoreId, string eqStatus)
        {
            var token = await GetTokenAsync();
            return await _minewService.GetDevicesFromCloud(token, cloudStoreId, eqStatus);
        }

        public async Task<MinewTemplateResponse> GetTemplatesFromCloudAsync(string cloudStoreId)
        {
            var token = await GetTokenAsync();
            return await _minewService.GetTemplatesFromCloud(token, cloudStoreId);
        }

        public async Task<string> GetUnboundTemplatePreviewAsync(string templateId)
        {
            var token = await GetTokenAsync();
            return await _minewService.GetUnboundTemplatePreview(token, templateId);
        }

        public async Task<string> GetBoundTemplatePreviewAsync(string templateId, string mac, string storeId)
        {
            var token = await GetTokenAsync();
            return await _minewService.GetBoundTemplatePreview(token, templateId, mac, storeId);
        }

        public async Task<dynamic> LightUpDeviceAsync(string mac, string storeId, int color, int total, int period, int interval, int brightness)
        {
            var token = await GetTokenAsync();
            return await _minewService.LightUpDevice(token, mac, storeId, color, total, period, interval, brightness);
        }

        public async Task<JsonDocument> BindDataAsync(object bindRequest)
        {
            var token = await GetTokenAsync();
            return await _minewService.BindData(token, bindRequest);
        }
    }
}
