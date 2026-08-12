using System;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceTemplateComboDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = "TEMPLATE";
        public long DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int? DeviceHeight { get; set; }
        public int? DeviceWidth { get; set; }
        public string DeviceMac { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public decimal? ScreenInch { get; set; }
        public int? Battery { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }

        public string? Color { get; set; }
        public int? Orientation { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedUser { get; set; }
        public int? UpdatedUser { get; set; }
        public bool IsActive { get; set; }
        public DeviceDto? Device { get; set; }

        public string? Error { get; set; }
    }
    public class UpdateDeviceTemplateComboDto
    {
        [Required(ErrorMessage = "DeviceId is required")]
        public int DeviceId { get; set; }

        [Required(ErrorMessage = "TemplateId is required")]
        public string TemplateId { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
