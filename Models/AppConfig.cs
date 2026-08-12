using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class AppConfig
    {
       // public AppConfig();

        public DateTime AddedDate { get; set; }
        public int AppConfigID { get; set; }
        [DefaultValue("")]
        [MaxLength(50)]
        public string ConfigName { get; set; }
        [DefaultValue("")]
        [MaxLength(300)]
        public string ConfigValue { get; set; }
        [DefaultValue("")]
        [MaxLength(200)]
        public string Description { get; set; }
        [DefaultValue("")]
        [MaxLength(50)]
        public string Remark { get; set; }
    }
}
