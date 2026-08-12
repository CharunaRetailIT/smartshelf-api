using System.ComponentModel.DataAnnotations;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class Status : BaseEntity
    {
        [Required, MaxLength(20)]
        public string EntityType { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
