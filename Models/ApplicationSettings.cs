using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class ApplicationSettings
    {
        public string JWT_Secret { get; set; }
        public string Client_URL { get; set; }
        public string Issuer { get; set; }
    }
}
