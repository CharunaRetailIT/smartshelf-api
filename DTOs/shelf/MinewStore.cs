using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewStore
    {
        public class MinewAddStoreRequest
        {
            public string number { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
            public string address { get; set; } = string.Empty;
        }

        public class MinewStoreResponse
        {
            public int code { get; set; }
            public string msg { get; set; }
            public List<MinewStoreItem>? data { get; set; }
        }

        public class MinewUpdateStoreRequest
        {
            public string id { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
            public string address { get; set; } = string.Empty;
            public int active { get; set; }
        }
        public class MinewStoreItem
        {
            public string id { get; set; }
            public string? uuid { get; set; }
            public string name { get; set; }
            public int active { get; set; }
            public string? country { get; set; }
            public string? province { get; set; }
            public string? city { get; set; }
            public string? address { get; set; }
            public string? serverIp { get; set; }
            public string? image { get; set; }
            public string? imageFullPath { get; set; }
            public string? description { get; set; }
            public string? latitude { get; set; }
            public string? longitude { get; set; }
            public string? createTime { get; set; }
            public string? updateTime { get; set; }
            public string? createBy { get; set; }
            public string? updateBy { get; set; }
            public string? merchantCode { get; set; }
            public string? remark { get; set; }
            public int deleteFlag { get; set; }
        }
    }
}
