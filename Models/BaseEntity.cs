using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;



namespace TERMS_LOYALTY_API.Models
{
    public class BaseEntity 
    {

        public BaseEntity()
        {
            GroupOfCompanyID = 1;
            CreatedUser = "Admin";
            CreatedDate = DateTime.Now;
            ModifiedUser = "Admin";
            ModifiedDate = DateTime.Now;
            DataTransfer = 0;
        }

        public int GroupOfCompanyID { get; set; }

        [MaxLength(50)]
        public string CreatedUser { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string ModifiedUser { get; set; }

        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModifiedDate { get; set; }

        [DefaultValue(0)]
        public int DataTransfer { get; set; }

    }
}
