using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class Jwt
    {
        public static string JwtKey = "Yh2k7QSu4l8CZg5p6X3Pna9L0Miy4D3Bvt0JVr87UcOj69Kqw5R2Nmf4FWs03Hdx";
        public static string JwtIssuer = "JWTAuthenticationServer";
        public static string JwtAudience = "JWTServicePostmanClient";
        public static string JwtSubject = "JWTServiceAccessToken";
    }
}
