using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API.Models
{
    public enum ResponseHttpMessage
    {
        IsSuccess = 200,
        IsUnSuccess = 201,
        ExceptionError = 2000,
        NoResultsFound = 100
    }
}
