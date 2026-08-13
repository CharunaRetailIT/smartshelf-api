using System.Collections.Generic;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IStore
    {
        Task<PagedResult<StoreMaster>> GetStoresAsync(StoreFilterDto filter);
        Task<StoreMaster> GetStoreByIdAsync(long id);
        Task<StoreMaster> CreateStoreAsync(StoreMaster store);
        Task<StoreMaster> UpdateStoreAsync(StoreMaster store);
        Task<bool> DeleteStoreAsync(long id);
        Task<bool> StoreCodeExistsAsync(string storeCode, long? excludeId = null);
        Task<List<StoreMaster>> GetStoresForSyncAsync(List<long> storeIds = null);
        Task<PagedResult<object>> GetStoreDetailsAsync(StoreFilterDto filter);
        Task<StoreMaster> GetStoreByMinewID(string Id);
        Task<StoreMaster?> GetStoreByProductIdAsync(long productId);

    }
}
