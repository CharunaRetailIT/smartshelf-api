using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IQueue
    {
        Task<PagedResult<QueueDto>> GetQueuesPagedAsync(QueuePagedRequest request);
        Task<QueueDetailDto> GetQueueByIdAsync(long id);
        Task<QueueDto> CreateDirectQueueAsync(CreateDirectQueueRequest request);
        Task<QueueDto> CreateQueueFromAssignmentAsync(CreateQueueFromAssignmentRequest request);
        Task<QueueDto> UpdateQueueAsync(long id, UpdateQueueRequest request);
        Task<bool> DeleteQueueAsync(long id);
        Task<QueueDto> ActivateQueueAsync(long id);
        Task<QueueDto> DeactivateQueueAsync(long id);
        Task<List<QueueDto>> GetUpcomingQueuesAsync(int hours = 24);
        Task<List<QueueDto>> GetActiveQueuesAsync();
        Task<List<QueueDto>> GetQueuesByDeviceAsync(long deviceId);
        Task<List<QueueDto>> GetQueuesByLocationAsync(string locationType, long locationId);
        Task<object> GetQueueStatisticsAsync(long? storeId);
        Task<List<PriorityMaster>> GetAllPriorities();

        //Task<QueueItemDto> CreateQueueItemAsync(CreateQueueItemDto dto, int userId);
        //Task<QueueItemDto> UpdateQueueItemAsync(long id, CreateQueueItemDto dto, int userId);
        //Task<bool> DeleteQueueItemAsync(long id, int userId);
        //Task<QueueItemDto> GetQueueItemByIdAsync(long id);
        //Task<IEnumerable<QueueItemDto>> GetQueueItemsAsync();
        //Task<IEnumerable<QueueItemDto>> GetPendingQueueItemsAsync();
        //Task<bool> UpdateQueueStatusAsync(long id, string status, string resultMessage = null);
        //Task AddExecutionLogAsync(long queueId, string status, string message = null, int? durationMs = null);
        //Task<IEnumerable<QueueExecutionLogDto>> GetExecutionLogsAsync(long queueId);
    }
}
