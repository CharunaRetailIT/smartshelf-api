using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserMasterController : BaseController
    {
      //  private readonly IUserMaster _IUserMaster;
        private readonly ILogger<UserMasterController> _logger;
        public UserMasterController(ILogger<UserMasterController> logger,IConfiguration configuration) : base(configuration)
        {
            _logger = logger;
           
            //  _IUserMaster = User_Master;
        }


        /// <summary>
        /// Every system user from the ERP database. POST despite taking no body.
        /// 
        /// Unrelated to SmartShelf sign-in, which is /api/auth.
        /// </summary>
        [HttpPost("GetSystemUsersDetails")]
        public async Task<HttpResponseData<UserMaster>> GetAllSystemUsers()
        {
            var result = new HttpResponseData<UserMaster>();
            try
            {
                result.Results = _IUserMaster.GetUsersDetails(connectionString);

                if (result.Results != null)
                    result.ResponsCode = (int)ResponseHttpMessage.IsSuccess;
                else
                {
                    result.ResponsCode = (int)ResponseHttpMessage.NoResultsFound;
                    result.Error = "No Results Found .";
                }


                _logger.LogError("Method: GetAllSystemUsers" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            catch (Exception ex)
            {
                result.ResponsCode = (int)ResponseHttpMessage.ExceptionError;
                result.Error = ex.Message;
                _logger.LogError("Method: GetAllSystemUsers" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// One ERP user by username.
        /// 
        /// Unrelated to SmartShelf sign-in, which is /api/auth.
        /// </summary>
        [HttpPost("GetUserDetails")]
        public async Task<HttpResponseData<ReturnUserDetails>> GetUserDetails(UserDetailRequest ObjUsr)
        {
            var result = new HttpResponseData<ReturnUserDetails>();
            try
            {
                result.Result = _IUserMaster.GetUserDetails(connectionString, ObjUsr.UserName);
                if (result.Result != null)
                    result.ResponsCode = (int)ResponseHttpMessage.IsSuccess;
                else
                {
                    result.ResponsCode = (int)ResponseHttpMessage.NoResultsFound;
                    result.Error = "No Results Found .";
                }


                _logger.LogError("Method: GetUserDetails" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            catch (Exception ex)
            {
                result.ResponsCode = (int)ResponseHttpMessage.ExceptionError;
                result.Error = ex.Message;
                _logger.LogError("Method: GetUserDetails" + " | " + "Error Message " + " | " + result.Error + " | " + "ResponsCode " + " | " + result.ResponsCode);
            }
            return await Task.FromResult(result);
        }


    }
}
