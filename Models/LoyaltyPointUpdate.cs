using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class LoyaltyEarnPointUpdate
    {
        public string MobileNo { get; set; }
        public decimal EarnPoint { get; set; }
       public DateTime TransactionDate { get; set; }
    }
}
