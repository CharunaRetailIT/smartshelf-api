using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewDeviceBatchAdd
    {
        public class MinewBatchAddRequest
        {
            [JsonPropertyName("storeId")]
            public string StoreId { get; set; }

            [JsonPropertyName("macArray")]
            public List<string> MacArray { get; set; }

            [JsonPropertyName("type")]
            public int Type { get; set; } = 1; // 1 is tag, 5 is warning light
        }
        public class MinewBatchAddResponse
        {
            public int Code { get; set; }
            public string Msg { get; set; }
            public Dictionary<string, string> Data { get; set; }
        }

        public class BatchAddDeviceRequest
        {
            public string StoreId { get; set; }
            public List<string> MacAddresses { get; set; }
            public int Type { get; set; } = 1;
            public int UserId { get; set; }
        }

        public class BatchAddResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int AddedCount { get; set; }
            public int FailedCount { get; set; }
            public Dictionary<string, string> Results { get; set; }
            public BatchWakeResult WakeUpResult { get; set; }
            public List<string> WokenDevices { get; set; } = new List<string>();
            public List<string> FailedToWakeDevices { get; set; } = new List<string>();
            public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        }

        public class MinewBatchWakeResponse
        {
            public int Code { get; set; }
            public string Msg { get; set; }
            public object Data { get; set; }
        }

        public class BatchWakeDevicesRequest
        {
            public string StoreId { get; set; }
            public List<string> MacAddresses { get; set; }
            public int UserId { get; set; }
        }

        public class BatchWakeResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int WokeCount { get; set; }
            public int FailedCount { get; set; }
        }
    }
}
