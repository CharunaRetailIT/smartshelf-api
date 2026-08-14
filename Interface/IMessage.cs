using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IMessage
    {
        Task<IEnumerable<MessageMaster>> GetMessagesAsync(long? storeId);
        Task<IEnumerable<MessageWithUserDto>> GetMessagesWithUsersAsync(HttpRequest request, long? storeId);
        Task<PagedResult<MessageWithUserPaged>> GetPagedMessagesAsync(MessagePagedRequest request, HttpRequest httpRequest);
        Task<MessageMaster> GetMessageByIdAsync(long id, long? storeId);
        Task<MessageMaster> CreateGeneralMessageAsync(MessageCreateDto dto);
        Task<MessageMaster> CreateImageMessageAsync(IFormFile file, string title, int duration, int createdBy, long? storeId, long? screenSizeId);
        Task<MessageMaster> CreateVideoMessageAsync(IFormFile file, string title, int duration, int createdBy, long? storeId, long? screenSizeId);
        Task<MessageMaster> CreateCustomImageMessageAsync(string title, string fabricJsData, string imageData, int duration, int createdBy, long? storeId, long? screenSizeId);
        Task<MessageMaster> UpdateMessageAsync(long id, MessageUpdateDto dto);
        Task<MessageMaster> UpdateGeneralMessage(UpdateGeneralMessageDto dto);
        Task<MessageMaster> UpdateImageMessage(long id, IFormFile image, string title, int? duration, bool? isActive, int updatedBy, long? storeId, long? screenSizeId);
        Task<MessageMaster> UpdateVideoMessage(long id, IFormFile video, string title, int duration, bool isActive, int updatedBy, long? storeId, long? screenSizeId);
        Task<MessageMaster> UpdateCustomImageMessage(UpdateCustomImageMessageDto dto);
        Task<bool> DeleteMessageAsync(long id, int userId, long? storeId);
        Task<List<MessageWithUserDto>> GetDevicesByIdsAsync(List<long> ids, long? storeId);
    }
}
