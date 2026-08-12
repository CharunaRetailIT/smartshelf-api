using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Services;

namespace TERMS_LOYALTY_API.Services.Providers
{
    // Seam between DeviceController and a specific ESL vendor's cloud API.
    // MinewEslProvider is the only implementation today; its methods still return
    // Minew-shaped DTOs (MinewDeviceResponse/MinewTemplateResponse) rather than a
    // normalized shape, since designing a normalized contract before a second
    // vendor's real API shape is known would just be guessing. When the next
    // brand is implemented, that's the point to introduce normalized DTOs.
    public interface IEslProvider
    {
        string BrandCode { get; }

        Task<MinewDeviceResponse> GetDevicesFromCloudAsync(string cloudStoreId, string eqStatus);

        Task<MinewTemplateResponse> GetTemplatesFromCloudAsync(string cloudStoreId);

        Task<string> GetUnboundTemplatePreviewAsync(string templateId);

        Task<string> GetBoundTemplatePreviewAsync(string templateId, string mac, string storeId);

        Task<dynamic> LightUpDeviceAsync(string mac, string storeId, int color, int total, int period, int interval, int brightness);

        Task<JsonDocument> BindDataAsync(object bindRequest);
    }
}
