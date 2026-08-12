using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.Models;

namespace TERMS_LOYALTY_API.Interface
{
   public interface ILoyaltyCustomer
    {
        public bool UpdateLoyaltyCustomer(string DBConnectionString,  LoyaltyEarnPointUpdate Epoint,ref decimal CurrentBalance, ref string CustomerCode);
        bool InsertLoyaltyCustomer(LoyaltyCustomerRequest loyaltyCustomer, string connectionString);
        IEnumerable<LoyaltyCustomerDetails> GetLoyaltyCustomerDetails(string DBConnectionString, long customerId);

    }
}
