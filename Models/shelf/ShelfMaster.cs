using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TERMS_LOYALTY_API.DTOs.shelf;

namespace TERMS_LOYALTY_API.Models.shelf
{
    [Table("ShelfMaster", Schema = "dbo")]
    public class ShelfMaster : BaseEntity
    {
        public long? AisleId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Coordinates { get; set; }
        public string Description { get; set; }
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSyncToCloud { get; set; } = false;

        [ForeignKey("AisleId")]
        [InverseProperty("Shelves")]
        [JsonIgnore]  
        public AisleMaster Aisle { get; set; }

        [ForeignKey(nameof(StoreId))]
        public virtual StoreMaster? Store { get; set; }

        //public List<ShelfAssignmentDto> Assignments { get; set; } = new();


        // public ICollection<ShelfProduct> ShelfProducts { get; set; }
    }
}
