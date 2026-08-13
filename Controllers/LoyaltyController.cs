using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.DTOs;
using TERMS_LOYALTY_API.Models;
using TERMS_MOBILE_WEB_API.Controllers;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Controllers
{
   // [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class LoyaltyController : BaseController
    {

        private readonly ILogger<LoyaltyController> _logger;
        public LoyaltyController(ILogger<LoyaltyController> logger, IConfiguration configuration) : base(configuration)
        {
            _logger = logger;

            //  _IUserMaster = User_Master;
           // _ILoyaltyCustomer = LoyaltyCustomer();
        }


        /// <summary>
        /// Applies a loyalty points transaction and returns the customer's new balance.
        /// 
        /// Legacy loyalty/ERP surface, unrelated to the SmartShelf ESL feature set. It runs
        /// against the separate ERP database via its own connection string.
        /// </summary>
        [HttpPost("UpdateLoyaltyPoints")]
        public async Task<HttpResponseData<ReturnUserDetails>> UpdateLoyaltyPoints(LoyaltyEarnPointUpdate ObjLEP)
        {
            var result = new HttpResponseData<ReturnUserDetails>();
            try
            {

                decimal CurrentBalance = 0; string CustomerCode ="";

                bool x = _ILoyaltyCustomer.UpdateLoyaltyCustomer(connectionString,ObjLEP, ref CurrentBalance,ref CustomerCode);
                if (x)
                {
                    //result.DocumentNo = DocumentNo;
                    result.MobileNo = ObjLEP.MobileNo;
                    result.CurrentBalance = CurrentBalance;
                    result.CustomerCode = CustomerCode;
                    result.ResponsCode = (int)ResponseHttpMessage.IsSuccess;
                }
                else
                {
                    //  result.Result = 0;
                    result.MobileNo ="";
                    result.CurrentBalance = 0;
                    result.CustomerCode = "";
                    result.ResponsCode = (int)ResponseHttpMessage.IsUnSuccess;

                }


                _logger.LogError("Method: UpdateLoyaltyPoints" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            catch (Exception ex)
            {
                result.ResponsCode = (int)ResponseHttpMessage.ExceptionError;
                result.Error = ex.Message;
                _logger.LogError("Method: UpdateLoyaltyPoints" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Registers a loyalty customer.
        /// 
        /// Legacy loyalty/ERP surface, unrelated to the SmartShelf ESL feature set. It runs
        /// against the separate ERP database via its own connection string.
        /// </summary>
        [HttpPost("CreateLoyaltyCustomer")]
        public async Task<HttpResponseData<ReturnUserDetails>> CreateLoyaltyCustomer([FromBody] LoyaltyCustomerRequest loyaltyCustomer)
        {
            var result = new HttpResponseData<ReturnUserDetails>();
            bool isInserted = _ILoyaltyCustomer.InsertLoyaltyCustomer(loyaltyCustomer, connectionString);
            try
            {
                if (isInserted)
                {
                    result.Success = true;
                    result.Message = "Customer inserted successfully.";
                }
                else
                {
                    result.Success = false;
                    result.Message = "Failed to insert customer.";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred: " + ex.Message;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Connectivity check - returns a fixed greeting. No data access.
        /// </summary>
        [HttpGet("employer")]
        public async Task<HttpResponseData<ReturnUserDetails>> employer()
        {
            var result = new HttpResponseData<ReturnUserDetails>();
            result.Success = true;
            result.Message = "Hello ";
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Loyalty customer record by id.
        /// 
        /// Legacy loyalty/ERP surface, unrelated to the SmartShelf ESL feature set. It runs
        /// against the separate ERP database via its own connection string.
        /// </summary>
        [HttpGet("GetLoyaltyCustomerDetails/{customerId:long}")]
        public async Task<HttpResponseData<List<LoyaltyCustomerDetails>>> GetLoyaltyCustomerDetails(long customerId)
        {
            var result = new HttpResponseData<List<LoyaltyCustomerDetails>>();
            try
            {
                // Call your method that retrieves the last 3 transactions
                var data = _ILoyaltyCustomer.GetLoyaltyCustomerDetails(connectionString, customerId).ToList();

                if (data != null && data.Any())
                {
                    result.Success = true;
                    result.Message = "Transactions retrieved successfully.";
                    result.Result = data;
                }
                else
                {
                    result.Success = false;
                    result.Message = "No transactions found for this customer.";
                    result.Result = new List<LoyaltyCustomerDetails>(); 
                }

                result.ResponsCode = (int)ResponseHttpMessage.IsSuccess;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred while retrieving loyalty customer details.";
                result.Error = ex.Message;
                result.ResponsCode = (int)ResponseHttpMessage.ExceptionError;
                _logger.LogError($"Method: GetLoyaltyCustomerDetails | Error: {ex.Message}");
            }

            return await Task.FromResult(result);
        }


    }
}
