using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class Company
    {
        public int CompanyID { get; set; }

        [Required]
        public int CostCentreID { get; set; }

        [Required]
        [MaxLength(15)]
        public string CompanyCode { get; set; }

        [Required]
        [MaxLength(50)]
        public string CompanyName { get; set; }

        [MaxLength(100)]
        public string OtherBusinessName1 { get; set; }

        [MaxLength(100)]
        [DefaultValue("")]
        public string OtherBusinessName2 { get; set; }

        [MaxLength(100)]
        [DefaultValue("")]
        public string OtherBusinessName3 { get; set; }

        [MaxLength(50)]
        public string Address1 { get; set; }

        [MaxLength(50)]
        public string Address2 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string Address3 { get; set; }

        [NotMapped]
        public string Address { get { return Address1 + "," + Address2 + "," + Address3; } }

        [DefaultValue("")]
        public string Telephone { get; set; }

        [DefaultValue("")]
        public string Mobile { get; set; }

        [DefaultValue("")]
        public string FaxNo { get; set; }

        [DefaultValue("")]
        public string Email { get; set; }

        [DefaultValue("")]
        public string WebAddress { get; set; }

        [DefaultValue("")]
        public string ContactPerson { get; set; }

        [DefaultValue(0)]
        public int TaxID1 { get; set; }

        [DefaultValue(0)]
        public int TaxID2 { get; set; }

        [DefaultValue(0)]
        public int TaxID3 { get; set; }

        [DefaultValue(0)]
        public int TaxID4 { get; set; }

        [DefaultValue(0)]
        public int TaxID5 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string TaxRegistrationNumber1 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string TaxRegistrationNumber2 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string TaxRegistrationNumber3 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string TaxRegistrationNumber4 { get; set; }

        [MaxLength(50)]
        [DefaultValue("")]
        public string TaxRegistrationNumber5 { get; set; }

        [Required]
        public int StartOfFiscalYear { get; set; }

        [Required]
        [MaxLength(50)]
        public string CostingMethod { get; set; }

        [DefaultValue(0)]
        public bool IsVat { get; set; }

        [DefaultValue(0)]
        public bool IsDelete { get; set; }

      //  public GroupOfCompany GroupOfCompany { get; set; }

       //public CostCentre CostCentre { get; set; }

        public virtual ICollection<Location> Locations { get; set; }



    }
}
