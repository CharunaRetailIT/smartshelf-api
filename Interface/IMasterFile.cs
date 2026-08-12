using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Interface
{
  public  interface IMasterFile
    {
        public List<Location> GetLocations(string DBConnectionString);
        //public List<Supplier> GetSuppliers(string DBConnectionString,string SupplierName);
    }
}
