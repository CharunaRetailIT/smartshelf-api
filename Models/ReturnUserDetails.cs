using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class ReturnUserDetails
    {
		public long UserMasterID { get; set; }
		public int CompanyID { get; set; }
		public int LocationID { get; set; }
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string LocationName { get; set; }
        public string LocationCode { get; set; }


    [Column(TypeName = "nvarchar(15)")]
		public string UserName { get; set; }

		[Column(TypeName = "nvarchar(100)")]
		public string UserDescription { get; set; }
		public string EmployeeCode { get; set; }
		public DateTime CreatedDate { get; set; }
        public ReturnUserPrivileges Return_UserPrivileges { get; set; }
    }


    public class ReturnUserPrivileges
    {        

        public long UserMasterID { get; set; }
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

        
    }


}
