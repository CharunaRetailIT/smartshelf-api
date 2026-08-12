using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Models;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Repository
{
    public class MasterRepository : IMasterFile
    {
        public MasterRepository()
        {

        }

        public List<Location> GetLocations(string DBConnectionString)
        {
            try
            {
              
                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;

                using (var entities = new DatabaseContext(contextOptions))
                {
                    //get all countries
                    return entities.Location.ToList();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

/*
        public List<Supplier> GetSuppliers(string DBConnectionString,string suppliername)
        {
            try
            {

                var contextOptions = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(DBConnectionString).Options;

                using (var entities = new DatabaseContext(contextOptions))
                {
                    if(suppliername.Trim()!= "")
                    //get all countries
                    return entities.Supplier.Where(x => x.SupplierName.Contains(suppliername) ).ToList();
                    else
                        return entities.Supplier.ToList();

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }*/
    }
}
