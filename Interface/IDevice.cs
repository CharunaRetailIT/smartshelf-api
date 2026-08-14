using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using static TERMS_LOYALTY_API.DTOs.shelf.AssignmentDto;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IDevice
    {
        //Device
        Task<PagedResult<DeviceDto>> GetDevicesPagedAsync(DevicePagedRequest request);
        Task<List<DeviceDto>> GetDevicesAsync(); // For backward compatibility
        Task<DeviceDto> GetDeviceByIdAsync(long deviceId);
        Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request);
        Task<DeviceDto> UpdateDeviceAsync(long id, UpdateDeviceRequest request);
        Task<(bool success, string message)> DeleteDeviceAsync(long id, int userId);
        Task<List<DeviceDto>> GetDevicesByIdsAsync(List<long> ids);

        // External integration: full device record + current bindings, keyed by MAC
        Task<DeviceDetailDto> GetDeviceDetailByMacAsync(string mac, long? storeId);

        //Esl Brand
        Task<List<EslBrandDto>> GetActiveBrandsAsync();


        //Device Template
        Task<PagedResult<TemplateDto>> GetTemplatesPagedAsync(TemplatePagedRequest request);
        Task<TemplateDto> GetTempalteByIdAsync(string templateId);
        Task<List<TemplateDto>> GetTemplatesAsync(); // For backward compatibility
        Task<(bool success, string message)> DeleteTemplateAsync(string id, int userId);
        Task<List<TemplateDto>> GetTemplatesByIdsAsync(List<string> ids);

        //Device Template Combos
        Task<PagedResult<DeviceTemplateComboDto>> GetCombosPagedAsync(DeviceTemplateComboPagedRequest request);
        Task<DeviceTemplateCombos> UpdateDeviceTemplateCombosAsync(long id, UpdateDeviceTemplateComboDto dto);
        Task<DeviceTemplateComboDto> GetComboByIdAsync(long comboId);
        Task<(bool success, string message)> DeleteComboAsync(long id, int userId);
        Task<(DeviceTemplateCombos Combo, bool AlreadyExisted)> CreateOrReuseTemplateComboAsync(long deviceId, string templateId, bool isDefault);

        //Device Message Combos
        Task<DeviceMessageCombos> GetByIdAsync(long id);
        Task<List<DeviceMessageCombos>> GetAllAsync();
        Task<PagedResult<DeviceMessageComboDto>> GetPagedAsync(DeviceMessageComboPagedRequest request);
        Task<DeviceMessageCombos> CreateAsync(CreateDeviceMessageComboDto dto);
        Task<DeviceMessageCombos> UpdateAsync(long id, UpdateDeviceMessageComboDto entity);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long deviceId, long messageId);
        Task<List<DeviceMessageCombos>> GetByDeviceIdAsync(long deviceId);
        Task<List<DeviceMessageCombos>> GetByMessageIdAsync(long messageId);
        Task<bool> DeactivateByDeviceIdAsync(long deviceId);
        Task<int> GetTotalCountAsync();
        Task<(bool success, string message)> DeleteMessageComboAsync(long id, int userId);


        // Bulk operations
        Task<List<DeviceMessageCombos>> CreateBulkAsync(List<CreateDeviceMessageComboDto> dtos);
        Task<bool> DeactivateMultipleAsync(List<long> ids);

        //Device Assignments
        Task<AssignmentDto> CreateAssignmentAsync(CreateAssignmentRequest request);
        Task<List<AssignmentDto>> GetAssignmentsAsync(string locationType, long locationId);
        Task<PagedResult<AssignmentViewModel>> GetAssignmentsPagedAsync(AssignmentPagedRequest request);
        Task<AssignmentDto> UpdateAssignmentOrderAsync(long id, UpdateAssignmentOrderRequest request);
        Task<bool> RemoveAssignmentAsync(long id, int userId);
        Task<bool> ValidateLocationExistsAsync(string locationType, long locationId);
        Task<string> GetLocationNameAsync(string locationType, long locationId);


        //Device Screen 
        Task<DeviceScreenDto> GetScreenByIdAsync(long id);
        Task<PagedResult<DeviceScreenDto>> GetPagedAsync(DeviceScreenPagedRequest request);
        Task<DeviceScreenDto> CreateScreenAsync(DeviceScreenCreateDto createDto);
        Task<DeviceScreenDto> UpdateScreenAsync(DeviceScreenUpdateDto updateDto);
        Task<bool> DeleteScreenAsync(long id);
        Task<bool> ToggleScreenActiveAsync(long id, int userId);
        Task<List<DeviceScreenDto>> GetAvailableScreensAsync(long? screenTypeId = null);
        Task<int> GetDeviceCountByScreenIdAsync(long screenId);
        Task<bool> CanDeleteScreenAsync(long screenId);


        // Gateway Methods
        Task<GatewayDto> GetGatewayByIdAsync(long id);
        Task<List<GatewayDto>> GetGatewaysByIdsAsync(List<long> ids);
        Task<PagedResult<GatewayDto>> GetGatewaysPagedAsync(GatewayPagedRequest request);
        Task<GatewayDto> CreateGatewayAsync(CreateGatewayRequest request);
        Task<GatewayDto> UpdateGatewayAsync(long id, UpdateGatewayRequest request);
        Task<(bool success, string message)> DeleteGatewayAsync(long id, int userId);
        Task SyncGatewaysFromCloudAsync(string token, string minewStoreId, long storeId);
        Task<dynamic> AddGatewayToMinewCloudAsync(string token, string mac, string name, string storeId);
        Task<dynamic> DeleteGatewayFromMinewCloudAsync(string token, string gatewayId, string storeId);
        Task<dynamic> UpdateGatewayInMinewCloudAsync(string token, string gatewayId, string name);
    }
}
