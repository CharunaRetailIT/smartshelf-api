namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewAddProductRequest
    {
        public string id { get; set; }
        public string storeId { get; set; }
        public string price { get; set; }
        public string barcode { get; set; }
        public string qrcode { get; set; }
        public string p_name { get; set; }
        public string p_code { get; set; }
        public string discount { get; set; }
    }
}
