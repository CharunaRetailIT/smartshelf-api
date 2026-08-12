using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TERMS_LOYALTY_API.DTOs;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("ProductAssignment", Schema = "dbo")]
    public class ProductAssignment : BaseEntity
    {
        public long AisleId { get; set; }
        public long? ShelfId { get; set; }
        public ShelfMaster Shelf { get; set; } =null;

        [ForeignKey(nameof(AisleId))]
        public AisleMaster AisleMaster { get; set; } = null;
        public long ProductId { get; set; }
        [DefaultValue(true)]
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(ProductId))]
        public ProductMaster Product { get; set; }

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }
    }
}
