using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using TERMS_MOBILE_WEB_API.Controllers;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/shelf")]
    [ApiController]
    [Authorize]

    public class ShelfController : ControllerBase
    {
        private readonly IShelf _shelfRepo;
        private readonly ILogger<ShelfController> _logger;
        public ShelfController(ILogger<ShelfController> logger,IShelf shelf)
        {
            _logger = logger;
            _shelfRepo = shelf;
        }

        /// <summary>
        /// Retrieves all shelves.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<List<ShelfMaster>>), 200)]
        public async Task<IActionResult> GetAllAsync(long? storeId)
        {
            var response = new HttpResponseData<List<ShelfMaster>>();
            try
            {
                var shelves = await _shelfRepo.GetAllAsync(storeId);
                response.Success = true;
                response.Message = "Shelves retrieved successfully.";
                response.Result = shelves.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shelves");
                response.Success = false;
                response.Message = "Failed to retrieve shelves.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);

            }
        }

        [HttpGet("with-assignments/{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ShelfFullDetails>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetShelfWithAssignmentsAsync(long id, long? storeId)
        {
            var response = new HttpResponseData<ShelfFullDetails>();

            try
            {
                var shelfWithAssignments = await _shelfRepo.GetShelfWithAssignmentsAsync(id,storeId);

                if (shelfWithAssignments == null)
                {
                    response.Success = false;
                    response.Message = $"Shelf with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Shelf with assignments retrieved successfully.";
                response.Result = shelfWithAssignments;
                response.ResponsCode = 200;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shelf with assignments for ID {ShelfId}", id);

                response.Success = false;
                response.Message = "Failed to retrieve shelf with assignments.";
                response.Error = ex.Message;
                response.ResponsCode = 500;

                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieves a shelf by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 404)]
        public async Task<IActionResult> GetByIdAsync(int id, long? storeId)
        {
            var response = new HttpResponseData<ShelfMaster>();
            try
            {
                var shelf = await _shelfRepo.GetByIdAsync(id,storeId);
                if (shelf == null)
                {
                    response.Success = false;
                    response.Message = $"Shelf with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Shelf retrieved successfully.";
                response.Result = shelf;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving shelf ID {id}");
                response.Success = false;
                response.Message = "Failed to retrieve shelf.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a new shelf.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 400)]
        public async Task<IActionResult> CreateAsync([FromBody] CreateShelfRequest request)
        {
            var response = new HttpResponseData<ShelfMaster>();
            if (!ModelState.IsValid)
            {
                response.Success = false;
                response.Message = "Invalid request data.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var shelf = new ShelfMaster
                {
                    Name = request.Name,
                    AisleId = request.AisleId,
                    Location = request.Location,
                    Coordinates = request.Coordinates,
                    StoreId = request.storeId,
                    Description = request.Description,
                    IsActive = true,
                    CreatedUser = request.CreatedUser,
                    CreatedDate = DateTime.Now,
                    UpdatedUser = request.CreatedUser,
                    UpdatedDate = DateTime.Now,
                };

                var result = await _shelfRepo.AddAsync(shelf);
                response.Success = true;
                response.Message = "Shelf created successfully.";
                response.Result = result;
                response.ResponsCode = 201;
                // return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Id }, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shelf");
                response.Success = false;
                response.Message = "Failed to create shelf.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing shelf.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<ShelfMaster>), 400)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateShelfRequest request)
        {
            var response = new HttpResponseData<ShelfMaster>();

            if (!ModelState.IsValid)
            {
                response.Success = false;
                response.Message = "Invalid request data.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var shelf = await _shelfRepo.GetByIdAsync(id, request.storeId);
                if (shelf == null)
                {
                    response.Success = false;
                    response.Message = $"Shelf with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                // Update only fields provided
                if (!string.IsNullOrWhiteSpace(request.Name)) shelf.Name = request.Name;
                if (!string.IsNullOrWhiteSpace(request.Location)) shelf.Location = request.Location;
                if (!string.IsNullOrWhiteSpace(request.Coordinates)) shelf.Coordinates = request.Coordinates;
                if (!string.IsNullOrWhiteSpace(request.Description)) shelf.Description = request.Description;
                if (request.IsActive.HasValue) shelf.IsActive = request.IsActive.Value;

                // Update audit fields
                shelf.UpdatedUser = request.ModifiedUser;
                shelf.UpdatedDate = DateTime.Now;

                var result = await _shelfRepo.UpdateAsync(shelf);

                response.Success = true;
                response.Message = "Shelf updated successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating shelf ID {id}");
                response.Success = false;
                response.Message = "Failed to update shelf.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a shelf by ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        public async Task<IActionResult> DeleteAsync(int id, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var deleted = await _shelfRepo.DeleteAsync(id, storeId);
                if (!deleted)
                {
                    response.Success = false;
                    response.Message = $"Shelf with ID {id} not found or already deleted.";
                    response.ResponsCode = 404;
                    response.Result = false;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Shelf deleted successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting shelf ID {id}");
                response.Success = false;
                response.Message = "Failed to delete shelf.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Assigns a product to a shelf.
        /// </summary>
        [HttpPost("{shelfId}/store/{storeId}/assign/{productId}/user/{userId}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> AssignProduct(int shelfId, int storeId, int productId,int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _shelfRepo.AssignProductAsync(shelfId, productId,storeId,userId);
                response.Success = true;
                response.Message = "Product assigned successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning product {productId} to shelf {shelfId}");
                response.Success = false;
                response.Message = "Failed to assign product.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Removes a product from a shelf.
        /// </summary>
        [HttpDelete("{shelfId}/store/{storeId}/remove/{productId}/user/{userId}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> RemoveProduct(int shelfId, int storeId,int productId, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _shelfRepo.RemoveProductAsync(shelfId, productId, storeId,userId);
                response.Success = true;
                response.Message = "Product removed successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing product {productId} from shelf {shelfId}");
                response.Success = false;
                response.Message = "Failed to remove product.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Assigns all products of a category to a shelf.
        /// </summary>
        [HttpPost("{shelfId}/store/{storeId}/assign/category/{categoryId}/user/{userId}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> AssignProductsByCategory(int shelfId, int storeId, int categoryId, int userId, [FromServices] IProduct productRepo)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var products = await productRepo.GetProductsByCategoryAsync(categoryId,storeId);
                foreach (var p in products)
                {
                    await _shelfRepo.AssignProductAsync(shelfId, p.Id,storeId,userId);
                }

                response.Success = true;
                response.Message = $"All products from category {categoryId} assigned to shelf {shelfId}.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning category {categoryId} products to shelf {shelfId}");
                response.Success = false;
                response.Message = "Failed to assign products by category.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
     
        /// <summary>
        /// Retrieves all products assigned to a shelf.
        /// </summary>
        [HttpGet("{shelfId}/store/{storeId}/products")]
        [ProducesResponseType(typeof(HttpResponseData<List<ProductViewDto>>), 200)]
        public async Task<IActionResult> GetProductsByShelf(int shelfId, int storeId,[FromServices] IProduct productRepo)
        {
            var response = new HttpResponseData<List<ProductViewDto>>();
            try
            {
                var products = await _shelfRepo.GetProductsByShelfAsync(shelfId,storeId);
                response.Success = true;
                response.Message = "Products retrieved successfully.";
                response.Result = products.ToList();
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving products for shelf {shelfId}");
                response.Success = false;
                response.Message = "Failed to retrieve products.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a shelf with audit user ID.
        /// </summary>
        [HttpDelete("{id}/store/{storeId}/user/{userId}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        public async Task<IActionResult> DeleteShelf(int id, int storeId, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _shelfRepo.DeleteShelfAsync(id, userId, storeId);
                response.Success = true;
                response.Message = "Shelf deleted successfully.";
                response.ResponsCode = 200;
                response.Result = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting shelf {id} by user {userId}");
                response.Success = false;
                response.Message = "Failed to delete shelf.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                response.Result = false;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Restores a soft-deleted shelf by ID and tracks the user performing the restoration.
        /// </summary>
        [HttpPut("{id:int}/restore/store/{storeId:int}/user/{userId:int}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> RestoreShelfAsync(int id,int storeId, int userId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                await _shelfRepo.RestoreShelfAsync(id, userId,storeId);
                response.Success = true;
                response.Message = $"Shelf {id} restored by user {userId}.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring shelf ID {id} by user {userId}");
                response.Success = false;
                response.Message = "Failed to restore shelf.";
                response.Result = false;
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
