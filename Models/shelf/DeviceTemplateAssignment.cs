using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class DeviceTemplateAssignment
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("DeviceTemplateComboId")]
        [Required]
        public long DeviceTemplateComboId { get; set; }

        [Column("LocationType")]
        [Required]
        [MaxLength(20)]
        public string LocationType { get; set; } // 'Aisle', 'Shelf', 'Product'

        [Column("LocationId")]
        [Required]
        public long LocationId { get; set; } // AisleMasterId, ShelfMasterId, or ProductMasterId

        [Column("DisplayOrder")]
        [Required]
        public int DisplayOrder { get; set; } = 0;

        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; } = true;

        [Column("CreatedDate")]
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }

        [Column("CreatedUser")]
        [Required]
        public int CreatedUser { get; set; }

        [Column("UpdatedUser")]
        public int? UpdatedUser { get; set; }

        // Navigation property
        [ForeignKey("DeviceTemplateComboId")]
        public virtual DeviceTemplateCombos DeviceTemplateCombo { get; set; }
    }
    // Enum for LocationType (optional but recommended)
    //public enum LocationType
    //{
    //    Aisle,
    //    Shelf,
    //    Product
    //}

    // Extension method to convert string to enum
    //public static class LocationTypeExtensions
    //{
    //    public static LocationType ToLocationTypeEnum(this string locationType)
    //    {
    //        return locationType switch
    //        {
    //            "Aisle" => LocationType.Aisle,
    //            "Shelf" => LocationType.Shelf,
    //            "Product" => LocationType.Product,
    //            _ => throw new ArgumentException($"Invalid LocationType: {locationType}")
    //        };
    //    }

    //    public static string ToLocationTypeString(this LocationType locationType)
    //    {
    //        return locationType.ToString();
    //    }
     //}
    }
