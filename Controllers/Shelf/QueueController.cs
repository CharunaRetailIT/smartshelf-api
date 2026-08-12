using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using TERMS_LOYALTY_API.Shared.Enum;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
    [Route("api/queue")]
    [ApiController]
    [Authorize]

    public class QueueController : ControllerBase
    {
        private readonly IQueue _queueRepo;
        private readonly ILogger<QueueController> _logger;
        private readonly SmartShelfDbContext _context;

        public QueueController(ILogger<QueueController> logger, IQueue queue, SmartShelfDbContext context)
        {
            _logger = logger;
            _queueRepo = queue;
            _context = context;
        }


        // GET: api/queue
        [HttpGet]
        [ProducesResponseType(typeof(HttpResponseData<PagedResult<QueueDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetAllQueues([FromQuery] QueuePagedRequest request)
        {
            try
            {
                var result = await _queueRepo.GetQueuesPagedAsync(request);

                return Ok(new HttpResponseData<PagedResult<QueueDto>>
                {
                    Success = true,
                    Message = "Queues retrieved successfully",
                    ResponsCode = 200,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queues");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve queues",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDetailDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetQueueById(long id)
        {
            try
            {
                var queue = await _queueRepo.GetQueueByIdAsync(id);

                if (queue == null)
                {
                    return NotFound(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = $"Queue with ID {id} not found",
                        ResponsCode = 404
                    });
                }

                return Ok(new HttpResponseData<QueueDetailDto>
                {
                    Success = true,
                    Message = "Queue retrieved successfully",
                    ResponsCode = 200,
                    Result = queue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving queue ID {id}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // POST: api/queue/direct
        [HttpPost("direct")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDto>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> CreateDirectQueue([FromBody] CreateDirectQueueRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Error = ModelState.Values.ToString(),
                        ResponsCode = 400
                    });
                }

                var result = await _queueRepo.CreateDirectQueueAsync(request);

                return CreatedAtAction(nameof(GetQueueById), new { id = result.Id },
                    new HttpResponseData<QueueDto>
                    {
                        Success = true,
                        Message = "Queue created successfully",
                        ResponsCode = 201,
                        Result = result
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 400
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating direct queue");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to create queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // POST: api/queue/from-assignment
        [HttpPost("from-assignment")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDto>), 201)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> CreateQueueFromAssignment([FromBody] CreateQueueFromAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Error = ModelState.Values.ToString(),
                        ResponsCode = 400
                    });
                }

                var result = await _queueRepo.CreateQueueFromAssignmentAsync(request);

                return CreatedAtAction(nameof(GetQueueById), new { id = result.Id },
                    new HttpResponseData<QueueDto>
                    {
                        Success = true,
                        Message = "Queue created successfully from assignment",
                        ResponsCode = 201,
                        Result = result
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating queue from assignment");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to create queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // PUT: api/queue/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> UpdateQueue(long id, [FromBody] UpdateQueueRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Error = ModelState.Values.ToString(),
                        ResponsCode = 400
                    });
                }

                var result = await _queueRepo.UpdateQueueAsync(id, request);

                return Ok(new HttpResponseData<QueueDto>
                {
                    Success = true,
                    Message = "Queue updated successfully",
                    ResponsCode = 200,
                    Result = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating queue ID {id}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to update queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // DELETE: api/queue/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> DeleteQueue(long id)
        {
            try
            {
                var success = await _queueRepo.DeleteQueueAsync(id);

                if (!success)
                {
                    return NotFound(new HttpResponseData<object>
                    {
                        Success = false,
                        Message = $"Queue with ID {id} not found",
                        ResponsCode = 404
                    });
                }

                return Ok(new HttpResponseData<object>
                {
                    Success = true,
                    Message = "Queue deleted successfully",
                    ResponsCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting queue ID {id}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to delete queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // POST: api/queue/{id}/activate
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> ActivateQueue(long id)
        {
            try
            {
                var result = await _queueRepo.ActivateQueueAsync(id);

                return Ok(new HttpResponseData<QueueDto>
                {
                    Success = true,
                    Message = "Queue activated successfully",
                    ResponsCode = 200,
                    Result = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating queue ID {id}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to activate queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // POST: api/queue/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(HttpResponseData<QueueDto>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 404)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> DeactivateQueue(long id)
        {
            try
            {
                var result = await _queueRepo.DeactivateQueueAsync(id);

                return Ok(new HttpResponseData<QueueDto>
                {
                    Success = true,
                    Message = "Queue deactivated successfully",
                    ResponsCode = 200,
                    Result = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating queue ID {id}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to deactivate queue",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/upcoming
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(HttpResponseData<List<QueueDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetUpcomingQueues([FromQuery] int hours = 24)
        {
            try
            {
                var queues = await _queueRepo.GetUpcomingQueuesAsync(hours);

                return Ok(new HttpResponseData<List<QueueDto>>
                {
                    Success = true,
                    Message = $"Upcoming queues for next {hours} hours retrieved",
                    ResponsCode = 200,
                    Result = queues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming queues");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve upcoming queues",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/active
        [HttpGet("active")]
        [ProducesResponseType(typeof(HttpResponseData<List<QueueDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetActiveQueues()
        {
            try
            {
                var queues = await _queueRepo.GetActiveQueuesAsync();

                return Ok(new HttpResponseData<List<QueueDto>>
                {
                    Success = true,
                    Message = "Active queues retrieved",
                    ResponsCode = 200,
                    Result = queues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active queues");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve active queues",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/device/{deviceId}
        [HttpGet("device/{deviceId}")]
        [ProducesResponseType(typeof(HttpResponseData<List<QueueDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetQueuesByDevice(long deviceId)
        {
            try
            {
                var queues = await _queueRepo.GetQueuesByDeviceAsync(deviceId);

                return Ok(new HttpResponseData<List<QueueDto>>
                {
                    Success = true,
                    Message = $"Queues for device {deviceId} retrieved",
                    ResponsCode = 200,
                    Result = queues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving queues for device {deviceId}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve queues",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/location/{locationType}/{locationId}
        [HttpGet("location/{locationType}/{locationId}")]
        [ProducesResponseType(typeof(HttpResponseData<List<QueueDto>>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetQueuesByLocation(string locationType, long locationId)
        {
            try
            {
                var queues = await _queueRepo.GetQueuesByLocationAsync(locationType, locationId);

                return Ok(new HttpResponseData<List<QueueDto>>
                {
                    Success = true,
                    Message = $"Queues for {locationType} {locationId} retrieved",
                    ResponsCode = 200,
                    Result = queues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving queues for location {locationType}/{locationId}");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve queues",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

        // GET: api/queue/stats
        [HttpGet("stats")]
        [ProducesResponseType(typeof(HttpResponseData<object>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetQueueStatistics([FromQuery] long? storeId = null)
        {
            try
            {
                var stats = await _queueRepo.GetQueueStatisticsAsync(storeId);

                return Ok(new HttpResponseData<object>
                {
                    Success = true,
                    Message = "Queue statistics retrieved",
                    ResponsCode = 200,
                    Result = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queue statistics");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve statistics",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }
        // GET: api/priority
        [HttpGet("prioritytypes")]
        [ProducesResponseType(typeof(HttpResponseData<PriorityMaster>), 200)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 400)]
        [ProducesResponseType(typeof(HttpResponseData<object>), 500)]
        public async Task<IActionResult> GetAllPriority()
        {
            try
            {
                var result = await _queueRepo.GetAllPriorities();

                return Ok(new HttpResponseData<PriorityMaster>
                {
                    Success = true,
                    Message = "Priority Types retrieved successfully",
                    ResponsCode = 200,
                    Results = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving priority types");
                return StatusCode(500, new HttpResponseData<object>
                {
                    Success = false,
                    Message = "Failed to retrieve priority types",
                    Error = ex.Message,
                    ResponsCode = 500
                });
            }
        }

    }
}
