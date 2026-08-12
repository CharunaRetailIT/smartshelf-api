using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Interface
{
    public interface IValidate
    {
        public bool CheckEmailExists(string DBConnectionString,string email);
        public bool CheckTelephoneExists(string DBConnectionString,string telephone);
        public bool CheckNICExists(string DBConnectionString,string nic);
        public string GetCompanyName(string connectionString);
    }
}
