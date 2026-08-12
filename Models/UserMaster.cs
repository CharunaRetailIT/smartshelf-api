using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{


	public class UserMaster
    {
		[Key]
		public long UserMasterID { get; set; }
		public int CompanyID { get; set; }
		public int LocationID { get; set; }

		[Column(TypeName = "nvarchar(15)")]
		public string UserName { get; set; }

		[Column(TypeName = "nvarchar(100)")]
		public string UserDescription { get; set; }

		[Column(TypeName = "nvarchar(100)")]
		public string Password { get; set; }
		public long UserGroupID { get; set; }
		public bool IsActive { get; set; }
		public bool IsUserCantChangePassword { get; set; }
		public bool IsUserMustChangePassword { get; set; }
		public bool IsDelete { get; set; }

		[Column(TypeName = "nvarchar(15)")]
		public string EmployeeCode { get; set; }
		public int GroupOfCompanyID { get; set; }

		[Column(TypeName = "nvarchar(50)")]
		public string CreatedUser { get; set; }

		public DateTime CreatedDate { get; set; }

		public string ModifiedUser { get; set; }

		public DateTime ModifiedDate { get; set; }

		public int DataTransfer { get; set; }
	}



	
	
	
	


		//public class UserInfo
  //      {
  //          public int UserId { get; set; }
  //          public string? DisplayName { get; set; }
  //          public string? UserName { get; set; }
  //          public string? Email { get; set; }
  //          public string? Password { get; set; }
  //          public DateTime? CreatedDate { get; set; }
  //      }
    
}
