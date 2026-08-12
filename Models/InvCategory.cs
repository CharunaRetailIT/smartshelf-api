using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models
{
    public class InvCategory : BaseEntity
    {

        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long InvCategoryID { get; set; }

        [Required]
        public long InvDepartmentID { get; set; }

        [Required]
        [StringLength(15)]
        public string CategoryCode { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryName { get; set; }

        [StringLength(150)]
        public string? Remark { get; set; }

        [Required]
        public bool IsNonExchangeable { get; set; }

        [Required]
        public bool IsDelete { get; set; }

        //[Required]
        //public int GroupOfCompanyID { get; set; }

        //[StringLength(50)]
        //public string? CreatedUser { get; set; }

        //[Required]
        //public DateTime CreatedDate { get; set; }

        //[StringLength(50)]
        //public string? ModifiedUser { get; set; }

        //[Required]
        //public DateTime ModifiedDate { get; set; }

        //[Required]
        //public int DataTransfer { get; set; }

    }
}
