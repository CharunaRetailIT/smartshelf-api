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


        /// <summary>
        /// Paged list of scheduled label updates, filterable by store, device, status and location.
        /// Status accepts Pending, Processing, Completed or Failed.
        /// </summary>
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

        /// <summary>
        /// One queue with its execution history.
        /// When a label did not change, this is where the reason is: ErrorMessage carries the
        /// vendor cloud's own rejection text and BindingData holds its raw response.
        /// </summary>
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

        /// <summary>
        /// Schedules a product onto a label for a time window.
        /// 
        /// The price is read when the queue FIRES, not when it is created - schedule a promotion
        /// today and the label picks up whatever the selling price is at that moment.
        /// 
        /// StartDate and EndDate are UTC. Sri Lanka is UTC+5:30, so a 3:00pm local promotion is
        /// sent as 09:30. Set EndDate: the overlap check cannot compare against an open-ended queue.
        /// 
        /// TemplateId is required for Minew devices even when the point is the message.
        /// Returns 409 when an unfinished queue already covers the same device and template.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
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

        /// <summary>
        /// Schedules a label that is already bound, so the device, template and product do not
        /// have to be repeated. AssignmentId is the TemplateAssignmentId returned by
        /// GET /api/products/by-code/{productCode}.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
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

        /// <summary>
        /// Reschedules a queue. Only the fields sent are applied; all are optional except UserId.
        /// Times are UTC, as on create.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
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

        /// <summary>
        /// Removes a schedule.
        /// Returns 409 while the queue is still displaying on its label - deactivate it first.
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
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
            catch (QueueDisplayActiveException ex)
            {
                // Still driving a label - a conflict the caller can resolve by
                // deactivating first, not a server fault.
                return Conflict(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 409
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

        /// <summary>
        /// Fires a queue immediately instead of waiting for its start time. Performs the real
        /// cloud bind, so the physical label repaints.
        /// 
        /// Returns 400 if the start time has not arrived, and 409 if the queue has already run -
        /// usually because the background processor got there first. Neither changes the queue's
        /// recorded outcome, so a Failed status always reflects a genuine bind attempt.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
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
            catch (QueueAlreadyActivatedException ex)
            {
                // The queue has already run - most often because the background
                // processor picked it up first. That is a conflict, not a server
                // fault, and the queue's own status is left as its real run left it.
                return Conflict(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 409
                });
            }
            catch (QueueNotDueException ex)
            {
                return BadRequest(new HttpResponseData<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ResponsCode = 400
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

        /// <summary>
        /// Retires a running queue. It stays Completed rather than reverting to Pending so the
        /// history stays truthful, and IsActive is cleared - which is also what makes it deletable.
        /// </summary>
        [Authorize(Roles = "Admin,Manager,Operator")]
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

        /// <summary>
        /// Queues that are armed and about to run within the next N hours.
        /// The quickest check that a schedule was accepted.
        /// </summary>
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

        /// <summary>
        /// Queues currently inside their display window.
        /// </summary>
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

        /// <summary>
        /// Everything scheduled against one label - useful when a label is showing something
        /// unexpected.
        /// </summary>
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

        /// <summary>
        /// Queues targeting one product or shelf. LocationType is PRODUCT or SHELF.
        /// </summary>
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

        /// <summary>
        /// Queue counts by status for a store - pending, completed and failed.
        /// </summary>
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
        /// <summary>
        /// The PriorityId values: 1 Emergency, 2 Price Change, 3 Promotion, 4 Scheduled (default),
        /// 5 Maintenance.
        /// </summary>
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
