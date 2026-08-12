using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewTemplate
    {
        public class MinewTemplateListRequest
        {
            public string storeId { get; set; }
            public int page { get; set; } = 1;
            public int size { get; set; } = 20;
            public string? condition { get; set; }
        }

        public class MinewTemplatePreviewUnboundRequest
        {
            public string templateId { get; set; }
            public Dictionary<string, object> data { get; set; } = new();
        }

        public class MinewTemplatePreviewBoundRequest
        {
            public string mac { get; set; }
            public string storeId { get; set; }
        }
    }
}
