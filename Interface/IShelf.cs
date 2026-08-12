using System.Collections.Generic;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IShelf
    {
        Task<IEnumerable<ShelfMaster>> GetAllAsync(long? storeId);
        Task<ShelfFullDetails?> GetShelfWithAssignmentsAsync(long id, long? storeId);
        Task<ShelfMaster> GetByIdAsync(long id, long? storeId);
        Task<ShelfMaster> AddAsync(ShelfMaster shelf);
        Task<ShelfMaster> UpdateAsync(ShelfMaster shelf);
        Task<bool> DeleteAsync(long id, long? storeId);
        Task AssignProductAsync(long shelfId, long productId, long? storeId, int userId);
        Task RemoveProductAsync(long shelfId, long productId, long? storeId,int userId);
        Task<IEnumerable<ProductViewDto>> GetProductsByShelfAsync(long shelfId, long? storeId);
        Task DeleteShelfAsync(long id, int user, long? storeId);
        Task RestoreShelfAsync(long id, int user, long? storeId);
        Task<IEnumerable<ShelfMaster>> GetUnsyncedShelf(long? storeId);
        Task SyncProducts(List<ShelfMaster> shelfs, long? storeId);

    }
}
