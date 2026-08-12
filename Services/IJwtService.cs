using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TERMS_LOYALTY_API.Models.user;
using TERMS_LOYALTY_API.Shared.Helpers;
using TERMS_MOBILE_WEB_API.Models;

namespace TERMS_LOYALTY_API.Services
{
    public interface IJwtService
    {
        string CreateToken(User user, string[] roles);

    }
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtService(IConfiguration config, IHttpContextAccessor httpContextAccessor) => (_config, _httpContextAccessor) = (config, httpContextAccessor);

        public string CreateToken(User user, string[] roles)
        {
            var request = _httpContextAccessor.HttpContext?.Request;

            string profileImageUrl = "";

            if (!string.IsNullOrEmpty(user.ProfileImagePath) && request != null)
            {
                profileImageUrl = request.ToAbsoluteUrl(user.ProfileImagePath);
            }

            var claims = new List<Claim>
            {
                new Claim("id", user.Id .ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.EmployeeId),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("departmentId", user.DepartmentId.ToString() ?? ""),
                new Claim("profileImageUrl", profileImageUrl ?? ""),
                new Claim("role", user.Role?.Name ?? "User")
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Use Jwt.JwtKey instead of appsettings
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwt.JwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Jwt.JwtIssuer,          // Use from Jwt class
                audience: Jwt.JwtAudience,      // Use from Jwt class  
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)); old api key

            //var keyString = _config["ApplicationSettings:JWT_Secret"];
            //if (string.IsNullOrEmpty(keyString))
            //    throw new InvalidOperationException("JWT Key is not configured.");

            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            //var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //var token = new JwtSecurityToken(
            //    issuer: _config["Jwt:Issuer"],
            //    audience: _config["Jwt:Audience"],
            //    claims: claims,
            //    expires: DateTime.UtcNow.AddHours(8),
            //    signingCredentials: creds
            //);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
