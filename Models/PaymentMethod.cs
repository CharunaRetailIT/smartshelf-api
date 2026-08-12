using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_LOYALTY_API.Models
{
    public class PaymentMethod : BaseEntity
    {
        public int PaymentMethodID { get; set; }

        [DefaultValue("")]
        [MaxLength(15)]
        public string PaymentMethodCode { get; set; }

        [DefaultValue("")]
        [MaxLength(50)]
        public string PaymentMethodName { get; set; }

        [DefaultValue(0)]
        public decimal CommissionRate { get; set; }

        [DefaultValue(0)]
        ////Cash	0
        ////Credit	1
        ////Cheque	2
        ////Credit Card	3
        public int PaymentType { get; set; }

        [DefaultValue(0)]
        public bool IsPaymentType { get; set; } //if paymenttype ->credit, then false

        [DefaultValue(0)]
        public bool IsReceiptType { get; set; } //if paymenttype ->credit, then false

        [DefaultValue(0)]
        public bool IsActive { get; set; }

        [DefaultValue(0)]
        public bool IsDelete { get; set; }

        public virtual ICollection<Customer> Customers { get; set; }

        ///set-off 5
        /// Third party 6


    }

    public enum ReferencePaymentType
    {
        Cash = 0,
        Credit = 1,
        Cheque = 2,
        CreditCard = 3
    }
}
