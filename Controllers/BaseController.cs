using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Repository;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Repository;

namespace TERMS_MOBILE_WEB_API.Controllers
{
    [ApiController]
    public class BaseController : Controller
    {

        protected string connectionString;
        public IConfiguration Configuration { get; }
        public IUserMaster _IUserMaster { get; }
        public  ILoyaltyCustomer  _ILoyaltyCustomer  { get; }
        public IValidate _IValidate { get; }
        //public IProduct _IProduct { get; }

        //public IShelf _Shelf { get; }
        public IProduct _Product { get; }
        //  public IPurchaseOrd IPurchaseOrdBase { get; }
        // public IPurchaseGRN IPurchaseGRNBase { get; }


        public BaseController(IConfiguration configuration)
        {
            Configuration = configuration;
            connectionString = Configuration["ConnectionStrings:DBConnection"];
            //_IUserMaster = new UserMasterRepository();
            // _IProduct = new ProductRepository();
            _IValidate = new ValidateRepository();
            //_Shelf = new ShelfRepository();
            //_Product = new ProductRepository();
            // IPurchaseOrdBase = new PurchaseOrdRepository();
            // IPurchaseGRNBase = new PurchaseGRNRepository();

        }

    }
}
