using System;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceMessageComboDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = "MESSAGE";

        public long DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int? DeviceHeight { get; set; }
        public int? DeviceWidth { get; set; }
        public string DeviceMac { get; set; }
        public string? DeviceIPAddress { get; set; }

        public long MessageId { get; set; }
        public string MessageTitle { get; set; }
        public string MessageContent { get; set; }
        public string? MessageType { get; set; }

        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public decimal? ScreenInch { get; set; }
        public int? Battery { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public DeviceDto? Device { get; set; }

        public string? StoreName { get; set; }
        public long? StoreId { get; set; }

        public string? Error { get; set; }

        //public long Id { get; set; }

        //[Required]
        //public long DeviceId { get; set; }

        //[Required]
        //public long MessageId { get; set; }

        //public bool IsActive { get; set; } = true;

        //public DateTime CreatedAt { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
    public class CreateDeviceMessageComboDto
    {
        [Required(ErrorMessage = "DeviceId is required")]
        public long DeviceId { get; set; }

        [Required(ErrorMessage = "MessageId is required")]
        public long MessageId { get; set; }
        public long StoreId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDeviceMessageComboDto
    {
        [Required(ErrorMessage = "DeviceId is required")]
        public long DeviceId { get; set; }

        [Required(ErrorMessage = "MessageId is required")]
        public long MessageId { get; set; }

        public bool IsActive { get; set; }
    }

    public class DeviceMessageComboPagedRequest : BasePagedRequest
    {
        public long? DeviceId { get; set; }
        public long? MessageId { get; set; }
        public long? StoreId { get; set; }
        public bool? IsActive { get; set; } = true;

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
