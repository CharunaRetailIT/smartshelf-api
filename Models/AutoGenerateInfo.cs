using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class AutoGenerateInfo
    {
        public long AutoGenerateInfoID { get; set; }

        [Required]
       
        public int ModuleType { get; set; }

        [NotMapped]
        public string StrModuleType { get; set; }


        [DefaultValue(0)]
    
        public int DocumentID { get; set; }

        [Required]
        public int FormId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FormName { get; set; }

        [Required]
        [MaxLength(100)]
        public string FormText { get; set; }

        [MaxLength(5)]
        public string Prefix { get; set; }

        [MaxLength(3)]
        public string Prefix2 { get; set; }

        [Required]
        public int CodeLength { get; set; }

        public int Suffix { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool AutoGenerete { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool AutoClear { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool IsDepend { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool IsDependCode { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool IsSupplierProduct { get; set; }

        [Required]
        [DefaultValue(1)]
        public bool IsOverWriteQty { get; set; }

        [DefaultValue(0)]
        public bool IsLocationCode { get; set; }

        [MaxLength(3)]
        public string ReportPrefix { get; set; }

        [Required]
        /*
         * 1 - Reference 
         * 2 - Transaction 
         * Etc.
         */
        public int ReportType { get; set; }

        [DefaultValue(0)]
        public bool PoIsMandatory { get; set; }

        [DefaultValue(0)]
        /*
         * true - Allow to recall Dispatch @ invoice
         *      - Not Allow to recall Invoice @ dispatch
         * false - not allow to recall Dispatch @ Invoice
         *       - Allow to recall Invoice @ dispatch
         */
        public bool IsDispatchRecall { get; set; }

        [DefaultValue(0)]
        public bool IsBackDated { get; set; }

        [NotMapped]
        public bool IsAccess { get; set; }

        [NotMapped]
        public bool IsPause { get; set; }

        [NotMapped]
        public bool IsSave { get; set; }

        [NotMapped]
        public bool IsModify { get; set; }

        [NotMapped]
        public bool IsView { get; set; }

        [DefaultValue(0)]
        public bool IsCard { get; set; } // supplier,customer,l.Customer,l.Supplier,Employee,

        [DefaultValue(0)]
        public int CardId { get; set; } //Supplier - 1, Customer - 2, Employee - 3- , [use referenceType table:-ReferenceTypeID]

        [DefaultValue(0)]
        public bool IsEntry { get; set; }

        [DefaultValue(0)]
        public bool IsSlabReport { get; set; }

        [DefaultValue(0)]
        public bool IsConsignment { get; set; }

        [DefaultValue(0)]
        public bool IsRoundOff { get; set; }

        [DefaultValue(0)]
        public bool IsAutoComplete { get; set; }

        [DefaultValue(0)]
        public bool IsUpdateProductImage { get; set; }

        [DefaultValue(0)]
        public bool IsAllowedInHO { get; set; }

        [DefaultValue(0)]
        public bool IsAllowedInOutlet { get; set; }

        [DefaultValue(0)]
        public bool IsActive { get; set; }

        [DefaultValue("")]
        public string Layout { get; set; }

        [DefaultValue("")]
        public string LayoutNew { get; set; }
        public int ReferenceDocumentID { get; set; }

        [DefaultValue("")]
        public string MenuName { get; set; }
    }
}
