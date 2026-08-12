using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class ApplicationUser
    {
        public long UserMasterID { get; set; }
        public int CompanyID { get; set; }
        public int LocationID { get; set; }

        [Column(TypeName = "nvarchar(15)")]
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
