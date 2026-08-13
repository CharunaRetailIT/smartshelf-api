using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceDto
    {
        public long Id { get; set; }
        public string Mac { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public long? ScreenId { get; set; }
        public decimal? ScreenSize { get; set; }
        public decimal? ScreenInch { get; set; }
        public int? ScreenHeight { get; set; }
        public int? ScreenWidth { get; set; }
        public string ScreenColor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? Battery { get; set; }
        public DateTime? LastSeen { get; set; }
        public long StoreId { get; set; }
        public bool IsOnline { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string NetworkName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Firmware { get; set; } = string.Empty;
        public string Hardware { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedUser { get; set; }
    }
    public class CreateDeviceRequest
    {
        // Minew labels report a bare 12-hex MAC (e1000005e79d) and that is what
        // the cloud and every existing DeviceMaster row use; the separated form
        // stays valid for hardware entered by hand.
        [MaxLength(50)]
        [RegularExpression(@"^(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$|^[0-9A-Fa-f]{12}$",
            ErrorMessage = "Invalid MAC address format")]
        public string MacAddress { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [RegularExpression("^(Minew|Standard)$")]
        public string DeviceType { get; set; }

        [Required]
        public long StoreId { get; set; }

        public long? ScreenId { get; set; }

        // A Minew device has no IP, and the form posts "" rather than omitting
        // the field - an empty string fails a bare regex, so allow it here.
        [MaxLength(50)]
        [RegularExpression(@"^$|^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$",
            ErrorMessage = "Invalid IP address format")]
        public string IpAddress { get; set; }

        [MaxLength(50)]
        public string NetworkName { get; set; }

        [MaxLength(50)]
        public string Firmware { get; set; }

        [MaxLength(50)]
        public string Hardware { get; set; }

        [Range(0, 100)]
        public int? Battery { get; set; }

        [Range(1, 3)]
        public int? StatusId { get; set; }

        public bool? IsActive { get; set; }

        public int? CreatedUser { get; set; }
    }

    public class UpdateDeviceRequest
    {
        public long Id { get; set; }

        // Minew labels report a bare 12-hex MAC (e1000005e79d) and that is what
        // the cloud and every existing DeviceMaster row use; the separated form
        // stays valid for hardware entered by hand.
        [MaxLength(50)]
        [RegularExpression(@"^(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$|^[0-9A-Fa-f]{12}$",
            ErrorMessage = "Invalid MAC address format")]
        public string MacAddress { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(Minew|Standard)$")]
        public string DeviceType { get; set; }

        public long? StoreId { get; set; }

        public long? ScreenId { get; set; }

        // A Minew device has no IP, and the form posts "" rather than omitting
        // the field - an empty string fails a bare regex, so allow it here.
        [MaxLength(50)]
        [RegularExpression(@"^$|^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$",
            ErrorMessage = "Invalid IP address format")]
        public string IpAddress { get; set; }

        [MaxLength(50)]
        public string NetworkName { get; set; }

        [MaxLength(50)]
        public string Firmware { get; set; }

        [MaxLength(50)]
        public string Hardware { get; set; }

        [Range(0, 100)]
        public int? Battery { get; set; }

        [Range(1, 3)]
        public int? StatusId { get; set; }

        public bool? IsActive { get; set; }

        public int? UpdatedUser { get; set; }
    }

}
