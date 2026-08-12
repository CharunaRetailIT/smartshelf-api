using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class CustomerGroup : BaseEntity
    {
        public int CustomerGroupID { get; set; }

        [Required]
        [MaxLength(20)]
        public string CustomerGroupCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerGroupName { get; set; }

        [DefaultValue("")]
        [MaxLength(150)]
        public string Remark { get; set; }

        [DefaultValue(0)]
        public bool IsDelete { get; set; }

        [DefaultValue(0)]
        public decimal CashDiscount { get; set; }

        [DefaultValue(0)]
        public decimal CashDiscountPre { get; set; }

        public virtual ICollection<Customer> Customers { get; set; }

        [DefaultValue(0)]
        public int IsGLTransfer { get; set; }

        [DefaultValue(0)]
        public int PriceType { get; set; }

        [DefaultValue("SellingPrice")]
        public string AppliedPrice { get; set; }
        [DefaultValue(false)]
        public bool RequirPermision { get; set; }
        [DefaultValue(0)]
        public int CustomerNo { get; set; }
        [DefaultValue("")]
        public string PreFix { get; set; }
        [DefaultValue(5)]
        public int NoLen { get; set; }

    }
}
