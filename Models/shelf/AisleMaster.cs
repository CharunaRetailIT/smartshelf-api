using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("AisleMaster", Schema = "dbo")]
    public class AisleMaster : BaseEntity
    {
        [StringLength(75)]
        public string Name { get; set; }
        [StringLength(50)]
        public string Description { get; set; }
        [StringLength(75)]
        public string Location { get; set; }
        [StringLength(75)]
        public string Coordinates { get; set; }
        public long? StoreId { get; set; }
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;
        public ICollection<ShelfMaster> Shelves { get; set; }

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }
    }
}
