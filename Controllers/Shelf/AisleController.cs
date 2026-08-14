using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/aisle")]
    [ApiController]
    [Authorize]

    public class AisleController : ControllerBase
    {
        private readonly IAisle _aisleRepo;
        private readonly ILogger<AisleController> _logger;

        public AisleController(ILogger<AisleController> logger, IAisle aisle)
        {
            _logger = logger;
            _aisleRepo = aisle;
        }

        /// <summary>
        /// Retrieves all aisles.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<List<AisleMaster>>), 200)]
        public async Task<IActionResult> GetAllAsync(long? storeId)
        {
            var response = new HttpResponseData<List<AisleMaster>>();
            try
            {
                var aisles = await _aisleRepo.GetAllAsync(storeId);
                response.Success = true;
                response.Message = "Aisles retrieved successfully.";
                response.Result = aisles.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aisles");
                response.Success = false;
                response.Message = "Failed to retrieve aisles.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        //// GET: api/aisle
        //[HttpGet("with-shelves")]
        //public async Task<ActionResult<IEnumerable<object>>> GetAisleWithShelves()
        //{
        //    var aisles = await _aisleRepo.GetAilsewithShelves();
        //    return Ok(aisles);
        //}

        /// <summary>
        /// Retrieves all aisles along with their shelves.
        /// </summary>
        [HttpGet("with-shelves")]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<AisleMasterWithShelvesDto>>), 200)]
        public async Task<IActionResult> GetAisleFullDetailsPaginated( [FromQuery] int page = 1,[FromQuery] int pageSize = 10, [FromQuery] string? search = null,  [FromQuery] string? status = null, [FromQuery] long? storeId = null)
        {
            var response = new HttpResponseData<PagedResult<AisleMasterWithShelvesDto>>();
            try
            {
                var result = await _aisleRepo.GetAisleFullDetails(page, pageSize, search, status, storeId);

                response.Success = true;
                response.Message = "Aisles with shelves retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated aisles with shelves");
                response.Success = false;
                response.Message = "Failed to retrieve aisles with shelves.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }



        /// <summary>
        /// Retrieves a specific aisle by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 404)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var response = new HttpResponseData<AisleMaster>();
            try
            {
                var aisle = await _aisleRepo.GetByIdAsync(id);
                if (aisle == null)
                {
                    response.Success = false;
                    response.Message = $"Aisle with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Aisle retrieved successfully.";
                response.Result = aisle;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving aisle ID {id}");
                response.Success = false;
                response.Message = "Failed to retrieve aisle.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Creates a new aisle.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("create")]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 400)]
        public async Task<IActionResult> CreateAsync([FromBody] CreateAisleRequest request)
        {
            var response = new HttpResponseData<AisleMaster>();
            if (!ModelState.IsValid)
            {
                response.Success = false;
                response.Message = "Invalid request data.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var aisle = await _aisleRepo.CreateAisleAsync(request);
                response.Success = true;
                response.Message = "Aisle created successfully.";
                response.Result = aisle;
                response.ResponsCode = 201;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating aisle");
                response.Success = false;
                response.Message = "Failed to create aisle.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST: api/aisle
        /// <summary>
        /// Creates an aisle.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost]
        public async Task<ActionResult<AisleMaster>> Add([FromBody] AisleMaster aisle)
        {
            //var data = await _aisleRepo.AddAsync(aisle);
            //return Ok(data);
            var response = new HttpResponseData<AisleMaster>();

            try
            {
                var result = await _aisleRepo.AddAsync(aisle);
                response.Success = true;
                response.Message = "Ailse created successfully.";
                response.Result = result;
                response.ResponsCode = 201;
                return Ok(response);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error creating aisle");
                response.Success = false;
                response.Message = "Failed to create aisle.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing aisle.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<AisleMaster>), 404)]
        public async Task<IActionResult> UpdateAsync([FromBody] AisleMaster data)
        {
            var response = new HttpResponseData<AisleMaster>();
            try
            {
                var updatedAisle = await _aisleRepo.UpdateAsync(data);
                if (updatedAisle == null)
                {
                    response.Success = false;
                    response.Message = $"Aisle with ID {data.Id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Aisle updated successfully.";
                response.Result = updatedAisle;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating aisle ID {data.Id}");
                response.Success = false;
                response.Message = "Failed to update aisle.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes an aisle by ID.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var deleted = await _aisleRepo.DeleteAsync(id);
                if (!deleted)
                {
                    response.Success = false;
                    response.Message = $"Aisle with ID {id} not found or already deleted.";
                    response.ResponsCode = 404;
                    response.Result = false;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Aisle deleted successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting aisle ID {id}");
                response.Success = false;
                response.Message = "Failed to delete aisle.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes an aisle by ID and tracks the user performing the deletion.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id:int}/user/{userId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> DeleteWithUserAsync(int id, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _aisleRepo.DeleteAisleAsync(id, userId);
                response.Success = true;
                response.Message = $"Aisle {id} deleted by user {userId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting aisle ID {id} by user {userId}");
                response.Success = false;
                response.Message = "Failed to delete aisle.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Restores a soft-deleted aisle by ID and tracks the user performing the restoration.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}/restore/user/{userId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> RestoreAisleAsync(int id, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _aisleRepo.RestoreAisleAsync(id, userId);
                response.Success = true;
                response.Message = $"Aisle {id} restored by user {userId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring aisle ID {id} by user {userId}");
                response.Success = false;
                response.Message = "Failed to restore aisle.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Assigns a product to an aisle.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("{aisleId:int}/assign/{productId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> AssignProductAsync(int aisleId, int productId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _aisleRepo.AssignProductAsync(aisleId, productId);
                response.Success = true;
                response.Message = $"Product {productId} assigned to aisle {aisleId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning product {productId} to aisle {aisleId}");
                response.Success = false;
                response.Message = "Failed to assign product.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Assigns all products from a category to an aisle.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("{aisleId:int}/store/{storeId:int}/assign/category/{categoryId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> AssignProductsByCategoryAsync(int aisleId, int categoryId, int storeId, [FromServices] IProduct productRepo)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var products = await productRepo.GetProductsByCategoryAsync(categoryId,storeId);
                foreach (var product in products)
                {
                    await _aisleRepo.AssignProductAsync(aisleId, product.Id);
                }

                response.Success = true;
                response.Message = $"All products from category {categoryId} assigned to aisle {aisleId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning category {categoryId} to aisle {aisleId}");
                response.Success = false;
                response.Message = "Failed to assign products by category.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Removes a product from an aisle with user tracking.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{aisleId:int}/remove/{productId:int}/user/{userId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> RemoveProductAsync(int aisleId, int productId, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _aisleRepo.RemoveProductAsync(aisleId, productId, userId);
                response.Success = true;
                response.Message = $"Product {productId} removed from aisle {aisleId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing product {productId} from aisle {aisleId}");
                response.Success = false;
                response.Message = "Failed to remove product.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets products assigned to a specific aisle.
        /// </summary>
        [HttpGet("{aisleId:int}/products")]
        [ProducesResponseType(typeof(HttpResponseData<List<ProductViewDto>>), 200)]
        public async Task<IActionResult> GetProductsByAisleAsync(int aisleId)
        {
            var response = new HttpResponseData<List<ProductViewDto>>();
            try
            {
                var products = await _aisleRepo.GetProductsByAisleAsync(aisleId);
                response.Success = true;
                response.Message = "Products retrieved successfully.";
                response.Result = products.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving products for aisle {aisleId}");
                response.Success = false;
                response.Message = "Failed to retrieve products.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets shelves assigned to a specific aisle.
        /// </summary>
        [HttpGet("{aisleId:int}/shelves")]
        [ProducesResponseType(typeof(HttpResponseData<List<ShelfMaster>>), 200)]
        public async Task<IActionResult> GetShelvesByAisleAsync(int aisleId)
        {
            var response = new HttpResponseData<List<ShelfMaster>>();
            try
            {
                var shelves = await _aisleRepo.GetShelvesByAilse(aisleId);
                response.Success = true;
                response.Message = "Shelves retrieved successfully.";
                response.Result = shelves.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving shelves for aisle {aisleId}");
                response.Success = false;
                response.Message = "Failed to retrieve shelves.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets all unique assignment IDs (for tracking assignments).
        /// </summary>
        [HttpGet("assignment-ids")]
        [ProducesResponseType(typeof(HttpResponseData<AssignmentIdsResponse>), 200)]
        public async Task<IActionResult> GetUniqueAssignmentIdsAsync()
        {
            var response = new HttpResponseData<AssignmentIdsResponse>();
            try
            {
                var result = await _aisleRepo.GetUniqueAssignmentIdsAsync();
                response.Success = true;
                response.Message = "Assignment IDs retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assignment IDs");
                response.Success = false;
                response.Message = "Failed to retrieve assignment IDs.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets product summary based on aisle and shelf filters.
        /// </summary>
        [HttpPost("summary")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        public async Task<IActionResult> GetSummaryAsync([FromBody] FilterRequest request)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var summary = await _aisleRepo.GetProductSummaryAsync(request.AisleIds, request.ShelfIds);
                response.Success = true;
                response.Message = "Summary retrieved successfully.";
                response.Result = summary;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving summary");
                response.Success = false;
                response.Message = "Failed to retrieve summary.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

    }
}
