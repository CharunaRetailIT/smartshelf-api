using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Services;
using TERMS_MOBILE_WEB_API.Models;
using static TERMS_LOYALTY_API.DTOs.shelf.MiewProduct;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewBinding;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewLogin;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewStore;
using static TERMS_LOYALTY_API.DTOs.shelf.MinewTemplate;

namespace TERMS_LOYALTY_API.Controllers.Shelf
{
    [Route("api/minew-integration")]
    [ApiController]
    public class MinewIntegrationController : ControllerBase
    {
        private readonly IProduct _productRepo;
        private readonly ILogger<ShelfController> _logger;
        private readonly MinewCloudService _minewService;
        private readonly IStore _storeRepo;
        private readonly IShelf _shelfRepo;
        public MinewIntegrationController(ILogger<ShelfController> logger, IProduct product, MinewCloudService minewService, IStore storeRepo, IShelf shelfRepo )
        {
            _logger = logger;
            _productRepo = product;
            _minewService = minewService;
            _storeRepo = storeRepo;
            _shelfRepo = shelfRepo;
        }


        // =========================
        // #region LOGIN
        // =========================
        #region Login

        /// <summary>
        /// Authenticates against the Minew cloud and caches the token used by every other
        /// Minew call. Credentials come from configuration, not this request.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] MinewLoginRequest request)
        {
            var token = await _minewService.LoginAsync(request.username.Trim(), request.password.Trim());
            return Ok(new { token });
        }

        #endregion

        // =========================
        // #region STORE
        // =========================
        #region Store Management

        /// <summary>
        /// Gets all active or inactive stores.
        /// </summary>
        //[HttpGet("stores")]
        //public async Task<IActionResult> GetStores()
        //{
        //    try
        //    {
        //        var result = await _minewService.GetStoresAsync();
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        ///// <summary>
        ///// Gets all active or inactive stores.
        ///// </summary>
        //[HttpGet("store/list")]
        //public async Task<IActionResult> GetStores([FromQuery] int active = 1, [FromQuery] string? condition = null)
        //{
        //    try
        //    {
        //        var result = await _minewService.GetStoresAsync(active, condition);
        //        var storeList = await _storeRepo.GetAllAsync();
        //        // Extract valid StoreIds from storeList (assuming StoreId is string to match "id" in result)
        //        var validStoreIds = storeList
        //            .Select(s => s.StoreId?.ToString())
        //            .Where(id => !string.IsNullOrEmpty(id))
        //            .ToHashSet(); // For fast lookup

        //        // Filter result.data to only include stores whose id is in validStoreIds
        //        var filteredData = result.data?
        //            .Where(store => validStoreIds.Contains(store.id))
        //            .ToList();

        //        // Create new response with filtered data
        //        //var filteredResponse = new
        //        //{
        //        //    result.code,
        //        //    result.msg,
        //        //    Data = new List<object>()
        //        //};

        //        return Ok(filteredData);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        [HttpPost("store/add")]
        public async Task<IActionResult> AddStore([FromBody] MinewAddStoreRequest request)
        {
            var result = await _minewService.AddStoreAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Renames or updates a store in the Minew cloud.
        /// </summary>
        [HttpPut("store/update")]
        public async Task<IActionResult> UpdateStore([FromBody] MinewUpdateStoreRequest request)
        {
            var result = await _minewService.UpdateStoreAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Opens or closes a Minew store. active=1 opens, 0 closes. A closed store stops
        /// accepting binds and price pushes.
        /// </summary>
        [HttpGet("store/openOrClose")]
        public async Task<IActionResult> OpenOrClose([FromQuery] string storeId, [FromQuery] int active)
        {
            var result = await _minewService.OpenOrCloseStoreAsync(storeId, active);
            return Ok(result);
        }

        #endregion

        #region Products Management
        /// <summary>
        /// Get products from Minew Cloud.
        /// </summary>
        [HttpGet("product/list")]
        public async Task<IActionResult> GetProducts([FromQuery] string storeId, [FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string? condition = null)
        {
            try
            {
                var result = await _minewService.GetProductsAsync(storeId, page, size, condition);
                return Ok(result);  // Or parse JSON and return structured response
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update products from Minew Cloud.
        /// </summary>
        [HttpPut("product/update")]
        [HttpPost("store/products/updateBatch")]
        public async Task<IActionResult> UpdateProductsBatch([FromBody] MinewUpdateProductBatchRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.storeId) || request.goodsList == null || !request.goodsList.Any())
                return BadRequest(new { message = "storeId and goodsList are required." });

            try
            {
                var result = await _minewService.UpdateProductsInBatchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete products from Minew Cloud.
        /// </summary>
        [HttpPost("store/products/deleteBatch")]
        public async Task<IActionResult> DeleteProductsBatch([FromBody] MinewDeleteProductBatchRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.storeId) || request.idArray == null || !request.idArray.Any())
                return BadRequest(new { message = "storeId and idArray are required." });

            try
            {
                var result = await _minewService.DeleteProductsInBatchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        /// <summary>
        /// Get unsynced products and mark them as synced after JSON generation.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncAndMarkProducts([FromQuery] string storeId, [FromQuery] string? opcode)
        {
            if (string.IsNullOrEmpty(storeId))
                return BadRequest(new { message = "storeId and opcode are required." });

            var json = await _productRepo.BuildUnsyncedProductsJsonAsync(storeId, opcode);
            return Content(json, "application/json");
        }

        /// <summary>
        /// Pushes the store's shelves to the Minew cloud as goods records so shelf-level
        /// labels have something to bind to.
        /// </summary>
        [HttpPost("shelf/syncToCloud")]
        public async Task<IActionResult> SyncShelfToCloud([FromQuery] long storeId, [FromQuery] string? opcode)
        {
            try
            {
                                //Cached for the token lifetime; credentials come from the MinewLogin config section.
                var token = await _minewService.GetValidTokenAsync();
                if (token == null)
                    return BadRequest("Login failed, no token received.");

                //Get Store
                var store = await _storeRepo.GetStoreByIdAsync(storeId);
                if (store == null)
                    return BadRequest("No store found");

                // Get unsynced local shelfs
                var unsyncedShelfs = await _shelfRepo.GetUnsyncedShelf(storeId);

                 if (!unsyncedShelfs.Any())
                        return Ok(new { message = "No unsynced active shelfs found." });

                // Send each to cloud
                foreach (var p in unsyncedShelfs)
                {
                    var addReq = new MinewAddProductRequest
                    {
                        id = $"S-{p.Id}",
                        storeId = store.MinewStoreId,
                        price = "",
                        barcode = "",
                        p_name = "",
                        p_code = "",
                        qrcode = ""
                    };

                    var response = await _minewService.AddProductToStoreAsync(addReq);
                    Console.WriteLine($"Uploaded {p.Name}: {response}");
                }

                //Mark as synced
                await _shelfRepo.SyncProducts(unsyncedShelfs.ToList(),storeId);

                return Ok(new { message = "Shelfs synced successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Pushes every product not yet synced to the Minew cloud goods store, then marks
        /// them synced. Products must exist in the cloud before a label can bind to them -
        /// binding an unsynced product fails with 数据不存在.
        /// </summary>
        [HttpPost("syncToCloud")]
        public async Task<IActionResult> SyncProductsToCloud([FromQuery] long storeId, [FromQuery] string? opcode)
        {
            try
            {
                                //Cached for the token lifetime; credentials come from the MinewLogin config section.
                var token = await _minewService.GetValidTokenAsync();
                if (token == null)
                    return BadRequest("Login failed, no token received.");

                //Get Store
                var store = await _storeRepo.GetStoreByIdAsync(storeId);
                if (store == null)
                    return BadRequest("No store found");

                // Get unsynced local products
                var unsyncedProducts = await _productRepo.GetUnsyncedProductList(storeId);

                if (!unsyncedProducts.Any())
                    return Ok(new { message = "No unsynced active products found." });

                // 3️⃣ Send each to cloud
                foreach (var p in unsyncedProducts)
                {
                    var addReq = new MinewAddProductRequest
                    {
                        id = p.Id.ToString(),
                        storeId = store.MinewStoreId,
                        price = p.SellingPrice.ToString("0.00"),
                        barcode = p.BarCode ?? "",
                        p_name = p.ProductName,
                        p_code = p.ProductCode,
                        discount = p.DiscountPrice.ToString("0.00"),
                        qrcode = "http://minewtag.com"
                    };

                    var response = await _minewService.AddProductToStoreAsync(addReq);
                    Console.WriteLine($"Uploaded {p.ProductName}: {response}");
                }

                //Mark as synced
                await _productRepo.SyncProducts(unsyncedProducts.ToList(),storeId);

                return Ok(new { message = "Products synced successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Template Management

        /// <summary>
        /// 4.1 Get paginated list of templates for a store
        /// </summary>
        [HttpGet("template/list")]
        public async Task<IActionResult> GetTemplateList([FromQuery] MinewTemplateListRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.storeId))
                    return BadRequest(new { message = "storeId is required." });

                var result = await _minewService.GetTemplateListAsync(request);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTemplateList failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 4.2 Preview template with custom data (unbound)
        /// </summary>
        [HttpPost("template/preview/unbound")]
        public async Task<IActionResult> PreviewTemplateUnbound([FromBody] MinewTemplatePreviewUnboundRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.templateId))
                    return BadRequest(new { message = "templateId is required." });

                var result = await _minewService.PreviewTemplateUnboundAsync(request);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreviewTemplateUnbound failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 4.3 Preview template with currently bound tag data
        /// </summary>
        [HttpPost("template/preview/bound")]
        public async Task<IActionResult> PreviewTemplateBound([FromBody] MinewTemplatePreviewBoundRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.mac) || string.IsNullOrWhiteSpace(request.storeId))
                    return BadRequest(new { message = "mac and storeId are required." });

                var result = await _minewService.PreviewTemplateBoundAsync(request);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreviewTemplateBound failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Goods to Template Binding

        /// <summary>
        /// 6.9 Connect existing goods to template (batch, no screen refresh)
        /// </summary>
        [HttpPost("binding/batchBindWithTemplate")]
        public async Task<IActionResult> BatchBindGoodsToTemplate([FromBody] MinewBatchBindWithTemplateRequest request)
        {
            try
            {
                if (request?.bindList == null || !request.bindList.Any())
                    return BadRequest(new { message = "bindList is required and cannot be empty." });

                foreach (var item in request.bindList)
                {
                    if (string.IsNullOrWhiteSpace(item.mac) ||
                        string.IsNullOrWhiteSpace(item.goodsId) ||
                        string.IsNullOrWhiteSpace(item.storeId) ||
                        string.IsNullOrWhiteSpace(item.templateId))
                        return BadRequest(new { message = "mac, goodsId, storeId, and templateId are required for each item." });
                }

                var result = await _minewService.BatchBindGoodsToTemplateAsync(request);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BatchBindGoodsToTemplate failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 6.10 Connect goods to template + force screen update (brush)
        /// </summary>
        [HttpPost("binding/batchBindAndUpdateWithTemplate")]
        public async Task<IActionResult> BatchBindAndUpdateWithTemplate([FromBody] MinewBatchBindAndUpdateWithTemplateRequest request)
        {
            try
            {
                if (request?.bindList == null || !request.bindList.Any())
                    return BadRequest(new { message = "bindList is required and cannot be empty." });

                foreach (var item in request.bindList)
                {
                    if (string.IsNullOrWhiteSpace(item.mac) ||
                        string.IsNullOrWhiteSpace(item.goodsId) ||
                        string.IsNullOrWhiteSpace(item.storeId) ||
                        string.IsNullOrWhiteSpace(item.templateId))
                        return BadRequest(new { message = "mac, goodsId, storeId, and templateId are required." });
                }

                var result = await _minewService.BatchBindAndUpdateWithTemplateAsync(request);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BatchBindAndUpdateWithTemplate failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion
    }
}
