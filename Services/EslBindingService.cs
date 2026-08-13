using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Services.Providers;

namespace TERMS_LOYALTY_API.Services
{
    /// <summary>
    /// Puts a product onto a physical label in the vendor cloud.
    ///
    /// Saving an ESL assignment locally only records the intent - the label keeps
    /// showing whatever it showed before until this bind runs. Removing an
    /// assignment has always unbound the label automatically
    /// (DeviceRepository.UnbindFromMinewAsync), so binding is the matching half of
    /// that pair, and both now happen without a second call from the caller.
    ///
    /// The request shape is deliberately the same one QueueRepository.ExecuteMinewQueue
    /// sends, since that is the path proven to work against the live cloud.
    /// </summary>
    public class EslBindingService
    {
        private readonly SmartShelfDbContext _context;
        private readonly IEslProviderFactory _providerFactory;
        private readonly ILogger<EslBindingService> _logger;

        public EslBindingService(
            SmartShelfDbContext context,
            IEslProviderFactory providerFactory,
            ILogger<EslBindingService> logger)
        {
            _context = context;
            _providerFactory = providerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Binds one product to one label. Never throws: the product is already
        /// committed by the time this runs, and a cloud outage must not turn a
        /// successful save into a failure. The outcome comes back for the caller
        /// to report.
        /// </summary>
        public async Task<(bool Success, string Message)> BindProductAsync(long deviceId, string templateId, long productId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return (false, "No template on the assignment - nothing to render, so no bind was attempted.");

            try
            {
                var device = await _context.DeviceMaster.FirstOrDefaultAsync(d => d.Id == deviceId);
                if (device == null)
                    return (false, $"Device {deviceId} not found.");

                if (string.IsNullOrEmpty(device.MACAddress))
                    return (false, $"Device {deviceId} has no MAC address.");

                var store = await _context.StoreMaster.FirstOrDefaultAsync(s => s.Id == device.StoreId);
                if (store == null || string.IsNullOrEmpty(store.MinewStoreId))
                    return (false, "Store has no MinewStoreId - the label cannot be bound in the cloud.");

                var product = await _context.ProductMaster.FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null)
                    return (false, $"Product {productId} not found.");

                var bindRequest = new
                {
                    // MinewStoreId, not the local StoreId. Sending the local id makes
                    // the cloud answer 门店不存在 ("store does not exist").
                    storeId = store.MinewStoreId,
                    labelMac = device.MACAddress,
                    goodsMap = BuildGoodsMap(product),
                    demoIdMap = new Dictionary<string, string> { ["A"] = templateId },
                    color = 1,
                    total = 5,
                    period = 500,
                    interval = 900,
                    brightness = 100,
                    opCode = new Random().Next(1000000000, 2000000000)
                };

                var provider = _providerFactory.GetProvider(device.DeviceType ?? "Minew");
                var response = await provider.BindDataAsync(bindRequest);

                var raw = response?.RootElement.GetRawText() ?? string.Empty;

                // Minew reports failure in the body, not the HTTP status - the same
                // trap that once left rejected queue binds marked as Completed.
                if (!BindSucceeded(raw, out var message))
                {
                    _logger.LogWarning("Cloud bind rejected for device {DeviceId} / product {ProductId}: {Message}",
                        deviceId, productId, message);
                    return (false, $"Cloud rejected the bind: {message}");
                }

                _logger.LogInformation("Bound product {ProductId} to device {DeviceId} ({Mac})",
                    productId, deviceId, device.MACAddress);
                return (true, "Bound to label.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding product {ProductId} to device {DeviceId}", productId, deviceId);
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Binds every template-carrying assignment for a product and folds the
        /// results into one summary. Rows marked deleted are skipped - those are
        /// unbinds, which the assignment removal path already handles.
        /// </summary>
        public async Task<(int Bound, int Failed, string Summary)> BindAssignmentsAsync(
            long productId,
            IEnumerable<(long DeviceId, string TemplateId, bool IsDeleted)> assignments)
        {
            var bound = 0;
            var failed = 0;
            var problems = new List<string>();

            foreach (var assignment in assignments ?? Enumerable.Empty<(long, string, bool)>())
            {
                if (assignment.IsDeleted || string.IsNullOrWhiteSpace(assignment.TemplateId))
                    continue;

                var (success, message) = await BindProductAsync(assignment.DeviceId, assignment.TemplateId, productId);
                if (success)
                {
                    bound++;
                }
                else
                {
                    failed++;
                    problems.Add($"device {assignment.DeviceId}: {message}");
                }
            }

            if (bound == 0 && failed == 0)
                return (0, 0, null);

            var summary = failed == 0
                ? $"{bound} label(s) bound."
                : $"{bound} bound, {failed} failed - {string.Join("; ", problems)}";

            return (bound, failed, summary);
        }

        private static Dictionary<string, string> BuildGoodsMap(ProductMaster product)
        {
            return new Dictionary<string, string>
            {
                ["id"] = product.Id.ToString(),
                ["specification"] = "2.9",
                ["unit"] = "001f",
                ["price"] = product.SellingPrice.ToString("0.00"),
                ["memberPrice"] = "",
                ["origin"] = "",
                ["discount"] = product.DiscountPrice.ToString("0.00"),
                ["barcoode"] = product.BarCode ?? "",
                ["qrcode"] = "",
                ["p_name"] = product.ProductName,
                ["p_code"] = product.ProductCode ?? ""
            };
        }

        private static bool BindSucceeded(string rawResponse, out string message)
        {
            message = null;

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                message = "empty response";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawResponse);

                if (doc.RootElement.TryGetProperty("msg", out var msgEl))
                    message = msgEl.GetString();
                else if (doc.RootElement.TryGetProperty("message", out var msgEl2))
                    message = msgEl2.GetString();

                if (doc.RootElement.TryGetProperty("code", out var codeEl) && codeEl.TryGetInt32(out var code))
                    return code == 200;

                message ??= "no code in response";
                return false;
            }
            catch (JsonException)
            {
                message = rawResponse.Length > 200 ? rawResponse.Substring(0, 200) : rawResponse;
                return false;
            }
        }
    }
}
