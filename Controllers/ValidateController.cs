using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_MOBILE_WEB_API.Controllers;

namespace TERMS_LOYALTY_API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class ValidateController : BaseController
    {
        private readonly ILogger<ValidateController> _logger;
        public ValidateController(ILogger<ValidateController> logger, IConfiguration configuration) : base(configuration)
        {
            _logger = logger;
        }
        [HttpGet("IsEmailExists")]
        public async Task<IActionResult> IsEmailExists(string email)
        {
            bool exists = _IValidate.CheckEmailExists(connectionString, email);
            return Json(new { exists = exists });
        }

        [HttpGet("IsTelephoneExists")]
        public async Task<IActionResult> IsTelephoneExists(string telephone)
        {
            bool exists = _IValidate.CheckTelephoneExists(connectionString, telephone);
            return Json(new { exists = exists });
        }

        [HttpGet("IsNICExists")]
        public async Task<IActionResult> IsNICExists(string nic)
        {
            bool exists = _IValidate.CheckNICExists(connectionString, nic);
            return Json(new { exists = exists });
        }

        [HttpGet("GetCompanyName")]
        public async Task<IActionResult> GetCompanyName()
        {
            string companyName = _IValidate.GetCompanyName(connectionString);
            return Json(new { companyName = companyName });
        }
    }
}
