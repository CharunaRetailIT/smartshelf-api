using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ProductSyncRequest
    {
        public string storeId { get; set; }
        public List<ProductSyncDto> goodsList { get; set; }
    }
}
