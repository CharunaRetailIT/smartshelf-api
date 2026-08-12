using System;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceScreenDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long ScreenTypeId { get; set; }
        public string ScreenTypeName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal Inch { get; set; }
        public string AspectRatio { get; set; }
        public int? DPI { get; set; }
        public int? RefreshRate { get; set; }
        public bool IsTouchScreen { get; set; }
        public string ColorDepth { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedUser { get; set; }
        public int? UpdatedUser { get; set; }

        // Statistics
        public int DeviceCount { get; set; }
        public bool IsInUse => DeviceCount > 0;
    }
    public class DeviceScreenCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public long ScreenTypeId { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Width { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Height { get; set; }

        [Required]
        [Range(0.1, 100)]
        public decimal Inch { get; set; }

        [MaxLength(10)]
        public string AspectRatio { get; set; }

        [Range(1, 1000)]
        public int? DPI { get; set; }

        [Range(1, 240)]
        public int? RefreshRate { get; set; } = 60;

        public bool IsTouchScreen { get; set; } = false;

        [MaxLength(20)]
        public string ColorDepth { get; set; } = "24-bit";

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string Description { get; set; }

        public int CreatedUser { get; set; }
    }

    // DeviceScreenUpdateDto.cs
    public class DeviceScreenUpdateDto
    {
        [Required]
        public long Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public long? ScreenTypeId { get; set; }

        [Range(1, 10000)]
        public int? Width { get; set; }

        [Range(1, 10000)]
        public int? Height { get; set; }

        [Range(0.1, 100)]
        public decimal? Inch { get; set; }

        [MaxLength(10)]
        public string AspectRatio { get; set; }

        [Range(1, 1000)]
        public int? DPI { get; set; }

        [Range(1, 240)]
        public int? RefreshRate { get; set; }

        public bool? IsTouchScreen { get; set; }

        [MaxLength(20)]
        public string ColorDepth { get; set; }

        public bool? IsActive { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public int UpdatedUser { get; set; }
    }
}
