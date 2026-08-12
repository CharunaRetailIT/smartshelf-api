using TERMS_LOYALTY_API.Models;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class TemplatePagedRequest: PagedRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public int? StoreId { get; set; } 
        public bool? IsActive { get; set; }
        public decimal? ScreenSize { get; set; }
        public string? Color { get; set; }
        public int? Orientation { get; set; }
    }
}
