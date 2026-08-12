using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class Employee : BaseEntity
    {
        public long EmployeeID { get; set; }

        [Required]
        [MaxLength(15)]
        public string EmployeeCode { get; set; }

        [Required]
        public int EmployeeTitle { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmployeeName { get; set; }

        [MaxLength(100)]
        public string Designation { get; set; }

        [Required]
        [DefaultValue(0)]
        public int Gender { get; set; }

        [Required]
        [MaxLength(25)]
        public string ReferenceNo { get; set; }

        [DefaultValue("")]
        [MaxLength(100)]
        public string Remark { get; set; }

        [Required]
        [MaxLength(100)]
        public string Address1 { get; set; }

        [Required]
        [MaxLength(100)]
        public string Address2 { get; set; }

        [DefaultValue("")]
        [MaxLength(100)]
        public string Address3 { get; set; }

        [DefaultValue("")]
        public string Email { get; set; }

        [DefaultValue("")]
        public string Telephone { get; set; }

        [Required]
        public string Mobile { get; set; }

        public byte[] Image { get; set; }

        [DefaultValue(0)]
        public bool IsActive { get; set; }

        [DefaultValue(0)]
        public decimal MinimumDiscount { get; set; }

        [DefaultValue(0)]
        public decimal MaximumDiscount { get; set; }

        [DefaultValue(0)]
        public decimal CrLimit { get; set; }

        [DefaultValue(0)]
        public decimal PchLimit { get; set; }

        [DefaultValue(0)]
        public decimal AccCredit { get; set; }

        [DefaultValue(0)]
        public decimal AccPch { get; set; }

        [DefaultValue(0)]
        public bool IsDelete { get; set; }

        [DefaultValue("")]
        [MaxLength(30)]
        public string Department { get; set; }

        [DefaultValue(0)]
        public int LocationID { get; set; }

        [DefaultValue(0)]
        public int CostCenterID { get; set; }


        [DefaultValue(0)]
        public bool IsAllowSMS { get; set; }

        [DefaultValue(0)]

        public bool IsAllowEmail { get; set; }

        [DefaultValue(0)]

        public long EmpVehicleID { get; set; }

    }
}
