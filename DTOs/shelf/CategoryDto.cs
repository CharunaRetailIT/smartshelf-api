using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CreateCategoryDto
    {
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryDescription { get; set; }
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryDescription { get; set; }
        public long? StoreId { get; set; }

        public bool IsActive { get; set; }
        public int? UpdatedUser { get; set; }
    }

    public class CategoryResponseDto
    {
        public long Id { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryDescription { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
