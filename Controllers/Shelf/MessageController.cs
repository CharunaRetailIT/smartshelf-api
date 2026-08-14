using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
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
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/message")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessage _messageRepo;
        private readonly ILogger<MessageController> _logger;
        public MessageController(ILogger<MessageController> logger, IMessage message)
        {
            _logger = logger;
            _messageRepo = message;
        }

        /// <summary>
        /// Get all messages.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<IEnumerable<MessageMaster>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<IEnumerable<MessageMaster>>), 500)]
        public async Task<IActionResult> GetMessagesAsync(long? storeId)
        {
            var response = new HttpResponseData<IEnumerable<MessageMaster>>();
            try
            {
                var messages = await _messageRepo.GetMessagesAsync(storeId);
                response.Success = true;
                response.Message = "Messages retrieved successfully.";
                response.Result = messages;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages");
                response.Success = false;
                response.Message = "Failed to retrieve messages.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get messages with user details.
        /// </summary>
        [HttpGet("with-users")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetMessagesWithUsersAsync(long? storeId)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var result = await _messageRepo.GetMessagesWithUsersAsync(Request, storeId);
                response.Success = true;
                response.Message = "Messages with user details retrieved successfully.";
                response.Result = result;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages with users");
                response.Success = false;
                response.Message = "Failed to retrieve messages with users.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Get paginated messages
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<MessageWithUserPaged>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetPaged([FromQuery] MessagePagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = "Invalid request parameters",
                        Error = ModelState.Values.ToString(),
                        ResponsCode = 400
                    });
                }

                var result = await _messageRepo.GetPagedMessagesAsync(request, Request);

                return Ok(new HttpResponseData<PagedResult<MessageWithUserPaged>>
                {
                    Result = result,
                    Success = true,
                    Message = $"Retrieved {result.Items.Count} of {result.TotalCount} messages",
                    ResponsCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated messages");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get a message by ID.
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetMessageAsync(long id, long? storeId)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var message = await _messageRepo.GetMessageByIdAsync(id, storeId);
                if (message == null)
                {
                    response.Success = false;
                    response.Message = $"Message with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Message retrieved successfully.";
                response.Result = message;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving message ID {id}");
                response.Success = false;
                response.Message = "Failed to retrieve message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Retrieves messages by a list of IDs.
        /// </summary>
        [HttpPost("messages/by-ids")]
        [ProducesResponseType(typeof(HttpResponseData<List<MessageWithUserDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<List<MessageWithUserDto>>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<List<MessageWithUserDto>>), 500)]
        public async Task<IActionResult> GetTemplatesByIds([FromBody] List<long> ids, long? storeId)
        {
            var response = new HttpResponseData<List<MessageWithUserDto>>();

            try
            {
                if (ids == null || !ids.Any())
                {
                    response.Success = false;
                    response.Message = "Message ID list is empty.";
                    response.ResponsCode = 400;
                    return BadRequest(response);
                }

                var devices = await _messageRepo.GetDevicesByIdsAsync(ids, storeId);

                if (devices == null || !devices.Any())
                {
                    response.Success = false;
                    response.Message = "No message found for the provided IDs.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Messages retrieved successfully.";
                response.Result = devices;
                response.ResponsCode = 200;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message by IDs");

                response.Success = false;
                response.Message = "Failed to retrieve messages.";
                response.Error = ex.Message;
                response.ResponsCode = 500;

                return Problem(
                    title: response.Message,
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }


        /// <summary>
        /// Create a general message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("general")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> CreateGeneralMessageAsync([FromBody] MessageCreateDto dto)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var message = await _messageRepo.CreateGeneralMessageAsync(dto);
                response.Success = true;
                response.Message = "General message created successfully.";
                response.Result = message;
                response.ResponsCode = 201;
                //return CreatedAtAction(nameof(GetMessageAsync), new { id = message.Id }, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating general message");
                response.Success = false;
                response.Message = "Failed to create general message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Upload an image message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("image")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UploadImageMessageAsync([FromForm] IFormFile image, [FromForm] string title,
                                                                 [FromForm] int duration, [FromForm] int createdBy, [FromForm] long? storeId, [FromForm] long? screenSizeId)
        {
            var response = new HttpResponseData<object>();
            if (image == null)
            {
                response.Success = false;
                response.Message = "No image uploaded.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var message = await _messageRepo.CreateImageMessageAsync(image, title, duration, createdBy, storeId,screenSizeId);
                response.Success = true;
                response.Message = "Image message uploaded successfully.";
                response.Result = message;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image message");
                response.Success = false;
                response.Message = "Failed to upload image message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Upload a video message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("video")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UploadVideoMessageAsync([FromForm] IFormFile video, [FromForm] string title,
                                                                 [FromForm] int duration, [FromForm] int createdBy, [FromForm] long? storeId, [FromForm] long? screenSizeId)
        {
            var response = new HttpResponseData<object>();
            if (video == null)
            {
                response.Success = false;
                response.Message = "No video uploaded.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var message = await _messageRepo.CreateVideoMessageAsync(video, title, duration, createdBy, storeId, screenSizeId);
                response.Success = true;
                response.Message = "Video message uploaded successfully.";
                response.Result = message;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video message");
                response.Success = false;
                response.Message = "Failed to upload video message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Create a custom image message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPost("custom-image")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> CreateCustomImageAsync([FromBody] CustomImageMessageDto dto)
        {
            var response = new HttpResponseData<object>();
            if (dto == null || string.IsNullOrEmpty(dto.ImageData))
            {
                response.Success = false;
                response.Message = "No image data provided.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var message = await _messageRepo.CreateCustomImageMessageAsync(
                    dto.Title,
                    JsonSerializer.Serialize(dto.FabricJsData),
                    dto.ImageData,
                    dto.Duration,
                    dto.CreatedBy,
                    dto.storeId,
                    dto.screenSizeId
                );
                response.Success = true;
                response.Message = "Custom image message created successfully.";
                response.Result = message;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom image message");
                response.Success = false;
                response.Message = "Failed to create custom image message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update general message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateMessage(long id, [FromBody] MessageUpdateDto dto)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var updatedMessage = await _messageRepo.UpdateMessageAsync(id, dto);
                if (updatedMessage == null)
                {
                    response.Success = false;
                    response.Message = $"Message with ID {id} not found.";
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Message updated successfully.";
                response.Result = updatedMessage;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating message ID {id}");
                response.Success = false;
                response.Message = "Failed to update message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update general message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("general/{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateGeneral(long id, [FromBody] UpdateGeneralMessageDto dto)
        {
            var response = new HttpResponseData<object>();
            if (id != dto.Id)
            {
                response.Success = false;
                response.Message = "ID mismatch.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var updated = await _messageRepo.UpdateGeneralMessage(dto);
                response.Success = true;
                response.Message = "General message updated successfully.";
                response.Result = updated;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating general message ID {id}");
                response.Success = false;
                response.Message = "Failed to update general message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update image message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("image/{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        // duration/isActive are nullable so that a form which omits them keeps the
        // stored value. As plain int/bool they bound to 0/false and were persisted,
        // which silently deactivated the message the caller was only replacing.
        public async Task<IActionResult> UpdateImage(long id, [FromForm] IFormFile image, [FromForm] string title,
                                                   [FromForm] int? duration, [FromForm] bool? isActive, [FromForm] int updatedBy, [FromForm] long? storeId, [FromForm] long? screenSizeId)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var updated = await _messageRepo.UpdateImageMessage(id, image, title, duration, isActive, updatedBy, storeId, screenSizeId);
                response.Success = true;
                response.Message = "Image message updated successfully.";
                response.Result = updated;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating image message ID {id}");
                response.Success = false;
                response.Message = "Failed to update image message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update video message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("video/{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateVideo(long id, [FromForm] IFormFile video, [FromForm] string title,
                                                     [FromForm] int duration, [FromForm] bool isActive, [FromForm] int updatedBy, [FromForm] long? storeId, [FromForm] long? screenSizeId)
        {
            var response = new HttpResponseData<object>();
            try
            {
                var updated = await _messageRepo.UpdateVideoMessage(id, video, title, duration, isActive, updatedBy, storeId, screenSizeId);
                response.Success = true;
                response.Message = "Video message updated successfully.";
                response.Result = updated;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating video message ID {id}");
                response.Success = false;
                response.Message = "Failed to update video message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update custom message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
        [HttpPut("custom-image/{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateCustomImage(long id, [FromBody] UpdateCustomImageMessageDto dto)
        {
            var response = new HttpResponseData<object>();
            if (id != dto.Id)
            {
                response.Success = false;
                response.Message = "ID mismatch.";
                response.ResponsCode = 400;
                return BadRequest(response);
            }

            try
            {
                var updated = await _messageRepo.UpdateCustomImageMessage(dto);
                response.Success = true;
                response.Message = "Custom image message updated successfully.";
                response.Result = updated;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating custom image message ID {id}");
                response.Success = false;
                response.Message = "Failed to update custom image message.";
                response.Error = ex.Message;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// DeleteDelete a message.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id:long}")]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<bool>), 500)]
        public async Task<IActionResult> DeleteMessageAsync(long id, [FromQuery] int userId, long? storeId)
        {
            var response = new HttpResponseData<bool>();
            try
            {
                var deleted = await _messageRepo.DeleteMessageAsync(id, userId, storeId);
                if (!deleted)
                {
                    response.Success = false;
                    response.Message = $"Message with ID {id} not found or already deleted.";
                    response.Result = false;
                    response.ResponsCode = 404;
                    return NotFound(response);
                }

                response.Success = true;
                response.Message = "Message deleted successfully.";
                response.Result = true;
                response.ResponsCode = 200;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message ID {id}");
                response.Success = false;
                response.Message = "Failed to delete message.";
                response.Error = ex.Message;
                response.Result = false;
                response.ResponsCode = 500;
                return Problem(title: response.Message, detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
