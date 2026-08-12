using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class DocumentNumber 
    {
        public long DocumentNumberID { get; set; }

        [DefaultValue(0)]
        public int DocumentID { get; set; }

        [DefaultValue("")]
        public string DocumentName { get; set; }

        [DefaultValue(0)]
        public int CompanyID { get; set; }

        [DefaultValue(0)]
        public int LocationID { get; set; }

        [DefaultValue(0)]
        public long DocumentNo { get; set; }

        [DefaultValue(0)]
        public long TempDocumentNo { get; set; }

        [DefaultValue(0)]
        public long TemplateDocumentNo { get; set; }

        [DefaultValue(0)]
        public int DocumentYear { get; set; }

        [DefaultValue("")]
        public string PrefixCode { get; set; }
    }
}
