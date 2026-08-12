using System;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class CreateSubCategoryDto
    {
        public long CategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryCode { get; set; }
        public string SubCategoryDescription { get; set; }
        public long? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedUser { get; set; }
    }

    public class UpdateSubCategoryDto
    {
        public long CategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryCode { get; set; }
        public string SubCategoryDescription { get; set; }
        public long? StoreId { get; set; }
        public bool IsActive { get; set; }
        public int? UpdatedUser { get; set; }
    }

    public class SubCategoryResponseDto
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        //public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryCode { get; set; }
        public string SubCategoryDescription { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
