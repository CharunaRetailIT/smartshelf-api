using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class GatewayDto
    {
        public long Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? MinewGatewayId { get; set; }
        public long StatusId { get; set; }
        public string? Status { get; set; }
        public string? GatewayType { get; set; }
        public string? HardwareVersion { get; set; }
        public string? FirmwareVersion { get; set; }
        public int? Battery { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedUser { get; set; }
        public string? UpdatedUser { get; set; }
    }

    public class AddGatewayToMinewRequest
    {
        public string Mac { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class CreateGatewayRequest
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long StoreId { get; set; }
        public string? GatewayType { get; set; } = "Minew";
        public string? HardwareVersion { get; set; }
        public string? FirmwareVersion { get; set; }
        public int? Battery { get; set; }
        public long StatusId { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
    }

    public class UpdateGatewayRequest
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Battery { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool IsActive { get; set; }
        public int UpdatedUser { get; set; }
    }

    public class MinewGatewayResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; } = string.Empty;
        public List<MinewGatewayItem> Items { get; set; } = new List<MinewGatewayItem>();
    }

    public class MinewGatewayItem
    {
        public string Id { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Hardware { get; set; }
        public string? Firmware { get; set; }
        public string? Product { get; set; }
        public string? Bluetooth { get; set; }
        public string? WifiVersion { get; set; }
        public string? BleVersion { get; set; }
        public string? SubModel { get; set; }
        public int Mode { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public string? CreateTime { get; set; }
        public string? UpdateTime { get; set; }
        public string? Remark { get; set; }
    }

    public class MinewAddGatewayRequest
    {
        [JsonPropertyName("mac")]
        public string Mac { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;
    }

    public class MinewAddGatewayResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class MinewDeleteGatewayResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class MinewUpdateGatewayRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MinewUpdateGatewayResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
