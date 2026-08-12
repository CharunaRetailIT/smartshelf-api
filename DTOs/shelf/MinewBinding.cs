using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewUnbindResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class MinewBinding
    {
        public class MinewBindWithTemplateItem
        {
            public string mac { get; set; }         
            public string goodsId { get; set; }    
            public string storeId { get; set; }      
            public string templateId { get; set; }    
        }

        public class MinewBatchBindWithTemplateRequest
        {
            public List<MinewBindWithTemplateItem> bindList { get; set; } = new();
        }

        public class MinewBatchBindAndUpdateWithTemplateRequest
        {
            public List<MinewBindWithTemplateItem> bindList { get; set; } = new();
            public bool brush { get; set; } = true;
        }
    }
}
