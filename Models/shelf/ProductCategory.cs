using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("ProductCategory", Schema = "dbo")]
    public class ProductCategory : BaseEntity
    {
        [StringLength(50)]
        public string CategoryName { get; set; }
        [StringLength(50)]
        public string CategoryCode { get; set; }
        [StringLength(75)]
        public string CategoryDescription { get; set; } = string.Empty;
        [DefaultValue(true)]
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }
    }
}
