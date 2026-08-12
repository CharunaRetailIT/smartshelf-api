using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Repository
{
    public class ValidateRepository : IValidate
    {
        public bool CheckEmailExists(string DBConnectionString, string email)
        {
            try
            {
                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;
                using (var entities = new DatabaseContext(contextOptions))
                {
                    return entities.LoyaltyCustomer
                    .Any(d => d.Email.Trim().ToLower() == email.Trim().ToLower());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool CheckNICExists(string DBConnectionString, string nic)
        {
            try
            {
                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;
                using (var entities = new DatabaseContext(contextOptions))
                {
                    return entities.LoyaltyCustomer
                    .Any(d => d.NicNo.Trim().ToLower() == nic.Trim().ToLower());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool CheckTelephoneExists(string DBConnectionString, string telephone)
        {
            try
            {
                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;
                using (var entities = new DatabaseContext(contextOptions))
                {
                    return entities.LoyaltyCustomer
                    .Any(d => d.Mobile.Trim().ToLower() == telephone.Trim().ToLower());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetCompanyName(string DBConnectionString)
        {
            try
            {
                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;
                using (var entities = new DatabaseContext(contextOptions))
                {
                    return entities.Company.Select(d => d.CompanyName).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
