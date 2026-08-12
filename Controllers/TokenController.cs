using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TERMS_MOBILE_WEB_API.Interface;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_MOBILE_WEB_API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : BaseController
    {
        
     //   private UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TokenController> _logger;
      
        public TokenController(ILogger<TokenController> logger,IConfiguration configuration) : base(configuration)
        {
          
            _logger = logger;
            //_IUserMaster = UserMasterRepository();


        }

        [HttpPost]
        public async Task<IActionResult> Post(ApplicationUser _userData)
        {
            var getResult = new HttpResponseData<ApplicationUser>();

            try
            {
                //$admin@2022
                //Set error log to Rquest

                if (_userData != null && _userData.UserName != null && _userData.Password != null)
                {
                    //Is Validate Token Author
                   // var applicationUser = await _IUserMaster.IsValidUser( _userData.UserName, _userData.Password, connectionString);

                    var result = new HttpResponseData<UserMaster>();
                    result.Result = _IUserMaster.IsValidUser(_userData.UserName, _userData.Password, connectionString);
                    if(result.Result == null)
                    {
                        result.ResponsCode = (int)ResponseHttpMessage.IsUnSuccess;
                        return await Task.FromResult(Unauthorized("Invalid credentials"));
                    }
                        result.ResponsCode = (int)ResponseHttpMessage.IsSuccess;
                    var applicationUser=  await Task.FromResult(result);

                    if (applicationUser.Result.UserName != null)
                    {
                        //create claims details based on the user information
                        var claims = new[] {
                        new Claim(JwtRegisteredClaimNames.Sub, Jwt.JwtSubject),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        new Claim("UserName", applicationUser.Result.UserName),
                        new Claim("Email",applicationUser.Result.LocationID.ToString()),
                        new Claim("Role", applicationUser.Result.CompanyID.ToString())
                    };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt.JwtKey));
                        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken(
                            Jwt.JwtIssuer,
                            Jwt.JwtAudience,
                            claims,
                            expires: DateTime.UtcNow.AddDays(1),
                            signingCredentials: signIn);

                        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
                        return await Task.FromResult(Ok(new { token = jwtToken }));

                    }
                    else
                    {
                        _logger.LogError("Method: TokenController Post" + " | " + "Invalid credentials");
                        return await Task.FromResult(BadRequest("Invalid credentials"));
                    }
                }
                else
                {
                    _logger.LogError("Method: TokenController Post" + " | " + "Invalid credentials");
                    return await Task.FromResult(BadRequest("Invalid credentials"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Method: TokenController Post" + " | " + ex.Message);
                return await Task.FromResult(BadRequest(ex.Message));
            }
        }
    }
}
