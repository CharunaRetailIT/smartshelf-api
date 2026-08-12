using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs
{
    public class LoyaltyCustomerDetails
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public byte[] CustomerImage { get; set; }

        public decimal PointBalance { get; set; }
        // Purchase summary
        public long TransactionID { get; set; }
        public DateTime TransactionDate { get; set; }

        public List<PurchasedItem> PurchasedItems { get; set; } = new();
    }

    public class PurchasedItem
    {
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
    }
}
