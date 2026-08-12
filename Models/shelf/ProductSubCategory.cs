using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("ProductSubCategory", Schema = "dbo")]
    public class ProductSubCategory : BaseEntity
    {
        public long CategoryId { get; set; }
        [StringLength(50)]
        public string SubCategoryName { get; set; }
        [StringLength(50)]
        public string SubCategoryCode { get; set; }
        [StringLength(75)]
        public string SubCategoryDescription { get; set; } = string.Empty;
        [DefaultValue(true)]
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }
    }
}
