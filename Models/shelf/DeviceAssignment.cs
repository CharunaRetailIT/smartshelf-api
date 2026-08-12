using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class DeviceAssignment: BaseEntity
    {       

            [Column("AssignmentType")]
            [Required]
            [MaxLength(20)]
            public string AssignmentType { get; set; } // 'TEMPLATE' or 'MESSAGE'

            [Column("DeviceTemplateComboId")]
            public long? DeviceTemplateComboId { get; set; }

            [Column("DeviceMessageComboId")]
            public long? DeviceMessageComboId { get; set; }

            [Column("LocationType")]
            [Required]
            [MaxLength(20)]
            public string LocationType { get; set; } // 'Aisle', 'Shelf', 'Product'

            [Column("LocationId")]
            [Required]
            public long LocationId { get; set; }

            [Column("DisplayOrder")]
            [Required]
            public int DisplayOrder { get; set; } = 0;

            [Column("StoreId")]
            [Required]
            public long StoreId { get; set; } = 0;


            [Column("IsActive")]
            [Required]
            public bool IsActive { get; set; } = true;

            // Navigation properties
            [ForeignKey("DeviceTemplateComboId")]
            public virtual DeviceTemplateCombos? DeviceTemplateCombo { get; set; }

            [ForeignKey("DeviceMessageComboId")]
            public virtual DeviceMessageCombos? DeviceMessageCombo { get; set; }
        }

        // Enums for better type safety
        public enum AssignmentType
        {
            TEMPLATE,
            MESSAGE
        }

        public enum LocationType
        {
            Aisle,
            Shelf,
            Product
        }

    public static class LocationTypeExtensions
    {
        public static LocationType ToLocationTypeEnum(this string locationType)
        {
            return locationType switch
            {
                "Aisle" => LocationType.Aisle,
                "Shelf" => LocationType.Shelf,
                "Product" => LocationType.Product,
                _ => throw new ArgumentException($"Invalid LocationType: {locationType}")
            };
        }

        public static string ToLocationTypeString(this LocationType locationType)
        {
            return locationType.ToString();
        }
    }
}
