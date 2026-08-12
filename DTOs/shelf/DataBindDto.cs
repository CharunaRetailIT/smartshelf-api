using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DataBindDto
    {
        public class BindDataRequest
        {
            public long ComboId { get; set; }
            public long? ProductId { get; set; } // Add this
            public Dictionary<string, string> GoodsMap { get; set; }
            public int? Color { get; set; }
            public int? Total { get; set; }
            public int? Period { get; set; }
            public int? Interval { get; set; }
            public int? Brightness { get; set; }
        }

        // Request model for unified binding
        public class UnifiedBindDataRequest
        {
            [Required]
            public int ComboId { get; set; }

            // Which table ComboId refers to: "TEMPLATE" -> DeviceTemplateCombos
            // (default, preserves existing callers), "MESSAGE" -> DeviceMessageCombos.
            // Both tables have independent identity columns, so the same numeric id
            // exists in each - without this the wrong row could be resolved and the
            // wrong label bound.
            public string ComboType { get; set; } = "TEMPLATE";

            // Binding type: "product" (default) or "shelf"
            public string BindingType { get; set; } = "product";

            // For product binding
            public int? ProductId { get; set; }
            public Dictionary<string, string> GoodsMap { get; set; }

            // For shelf binding
            public int? ShelfId { get; set; }
            public string ShelfName { get; set; }
            public string ShelfCode { get; set; }

            // Optional image binding for both types (0 = no image)
            public int MessageId { get; set; } = 0;

            // Display parameters
            public int? Color { get; set; } = 1;
            public int? Total { get; set; } = 5;
            public int? Period { get; set; } = 500;
            public int? Interval { get; set; } = 900;
            public int? Brightness { get; set; } = 100;
        }

        public class QuickBindRequest
        {
            public long ProductId { get; set; }
            public long ComboId { get; set; }
        }

        public class BatchBindAssignment
        {
            public long ComboId { get; set; }
            public long? ProductId { get; set; }
            public string? LocationType { get; set; }
            public long LocationId { get; set; }
            public Dictionary<string, string>? CustomData { get; set; }
            public int? Color { get; set; }
            public int? Brightness { get; set; }
        }

        public class BatchBindRequest
        {
            public List<BatchBindAssignment> Assignments { get; set; }
        }

        public class BatchBindResult
        {
            public long ComboId { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
