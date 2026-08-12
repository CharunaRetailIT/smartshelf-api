using System.Collections.Generic;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using static TERMS_LOYALTY_API.DTOs.shelf.ProductSummary;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IAisle
    {
        Task<IEnumerable<AisleMaster>> GetAllAsync(long? storeId);
        Task<AisleMaster> GetByIdAsync(long id);
        Task<AisleMaster> CreateAisleAsync(CreateAisleRequest request);

        Task<AisleMaster> AddAsync(AisleMaster data);
        Task<AisleMaster> UpdateAsync(AisleMaster data);
        Task<bool> DeleteAsync(long id);
        Task AssignProductAsync(long aisleId, long productId);
        Task RemoveProductAsync(long asileId, long productId,int userId);
        Task<IEnumerable<ProductViewDto>> GetProductsByAisleAsync(long aisleId);
        Task DeleteAisleAsync(long id, int user);
        Task RestoreAisleAsync(long id, int user);
        Task<IEnumerable<ShelfMaster>> GetShelvesByAilse(long id);
        //Task<IEnumerable<AisleMaster>> GetAilsewithShelves();
        Task<PagedResult<AisleMasterWithShelvesDto>> GetAisleFullDetails(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? storeId = null);
        Task<AssignmentIdsResponse> GetUniqueAssignmentIdsAsync();
        Task<ProductSummaryResponseDto> GetProductSummaryAsync(List<long> aisleIds, List<long> shelfIds);
    }
}
