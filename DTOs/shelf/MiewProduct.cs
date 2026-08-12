using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MiewProduct
    {
        public class MinewUpdateProductItem
        {
            public string? id { get; set; }          // Product ID (required)
            public string? code { get; set; }        // Product code
            public string? name { get; set; }        // Product name
            public string? quantity { get; set; }    // Quantity
            public string? specification { get; set; } // Specification
            public string? supplier { get; set; }    // Supplier
        }

        public class MinewUpdateProductBatchRequest
        {
            public string storeId { get; set; } = string.Empty;
            public List<MinewUpdateProductItem> goodsList { get; set; } = new();
            public string? opCode { get; set; }      // Optional: custom operation code
        }

        public class MinewUpdateProductBatchResponse
        {
            public int code { get; set; }
            public string? msg { get; set; }
            public object? data { get; set; }
        }
        public class MinewDeleteProductBatchRequest
        {
            public List<string> idArray { get; set; } = new();
            public string storeId { get; set; } = string.Empty;
        }

        public class MinewDeleteProductBatchResponse
        {
            public int code { get; set; }
            public string? msg { get; set; }
            public object? data { get; set; }
        }

        // Add these DTOs to your existing DTOs
        public class MinewUpdateProductRequest
        {
            public string id { get; set; }                // Product ID (string)
            public string storeId { get; set; }           // Store ID
            public string price { get; set; }             // Selling price
            public string barcode { get; set; }           // Barcode
            public string p_name { get; set; }            // Product name
            public string p_code { get; set; }            // Product code
            public string discount { get; set; }          // Discount price
            public string qrcode { get; set; }            // QR code URL
                                                          // Optional fields based on your template
            public string specification { get; set; }     // Specification
            public string unit { get; set; }              // Unit
            public string memberPrice { get; set; }       // Member price
            public string origin { get; set; }            // Origin
            public string image { get; set; }             // Image URL
            public string barcoode { get; set; }          // Note: typo "barcoode" (if required by API)
        }

        public class MinewUpdateProductResponse
        {
            public int code { get; set; }
            public string message { get; set; }
            public object data { get; set; }
            public string msg { get; set; } // Some endpoints use "msg" instead of "message"
        }
    }
}
