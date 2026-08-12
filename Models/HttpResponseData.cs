using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class HttpResponseData<T>
    {
        public T Result { get; set; }
        public List<T> Results { get; set; }
        public int ResponsCode { get; set; }
        public string Error { get; set; }
        public string DocumentNo { get; set; }
        public string CustomerCode { get; set; }
        public decimal CurrentBalance { get; set; }
        public string MobileNo { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

    }
}
