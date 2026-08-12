using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceTemplateComboViewModel
    {
        public long Id { get; set; }
        public long DeviceId { get; set; }
        public string DeviceMac { get; set; }
        public string TemplateId { get; set; }
        public string DeviceName { get; set; }
        public string TemplateName { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public decimal? ScreenInch { get; set; }
        public int? Battery { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
