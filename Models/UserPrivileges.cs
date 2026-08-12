using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class UserPrivileges 
    {

        public long UserPrivilegesID { get; set; }

        public long UserMasterID { get; set; }

        //[MaxLength(15)]
        //[NotMapped]
        //public string User_Name { get; set; }

        [MaxLength(50)]
        [NotMapped]
        public string UserDescription { get; set; }

        [MaxLength(15)]
        [NotMapped]
        public string UserPassword { get; set; }

        public long TransactionRightsID { get; set; }

        public long FormID { get; set; }

        [MaxLength(15)]
        [NotMapped]
        public string TransactionCode { get; set; }

        [MaxLength(50)]
        [NotMapped]
        public string TransactionName { get; set; }

        [MaxLength(50)]
        [NotMapped]
        public string FormName { get; set; }

        [DefaultValue(0)]
        public int TransactionTypeID { get; set; } // 1- inventory, 2 - Distribution, 3 - Manufacturing, 4 - Finance, 5 - HR, 6 - Logistic, 7- CRM

        [DefaultValue(0)]
        public bool IsAccess { get; set; }

        [DefaultValue(0)]
        public bool IsPause { get; set; }

        [DefaultValue(0)]
        public bool IsSave { get; set; }

        [DefaultValue(0)]
        public bool IsModify { get; set; }

        [DefaultValue(0)]
        public bool IsView { get; set; }

        [DefaultValue("")]
        public string Layout { get; set; }

       
        public virtual UserMaster UserName { get; set; }
    }
}
