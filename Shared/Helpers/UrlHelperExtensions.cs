using Microsoft.AspNetCore.Http;

namespace TERMS_LOYALTY_API.Shared.Helpers
{
    public static class UrlHelperExtensions
    {
        public static string ToAbsoluteUrl(this HttpRequest request, string relativePath)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}{relativePath}";
        }
    }
}
