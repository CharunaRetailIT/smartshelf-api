using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class MinewTemplates
    {
        [Key]
        [Column("Id")]
        [Required]
        [MaxLength(100)]
        public string Id { get; set; }

        [Column("Name")]
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Column("Description")]
        [MaxLength(500)]
        public string Description { get; set; }

        [Column("ScreenInch")]
        public decimal? ScreenInch { get; set; }

        [Column("ScreenWidth")]
        public int? ScreenWidth { get; set; }

        [Column("ScreenHeight")]
        public int? ScreenHeight { get; set; }

        [Column("Color")]
        [MaxLength(50)]
        public string Color { get; set; }

        [Column("Orientation")]
        public int? Orientation { get; set; }

        [Column("PreviewImage")]
        [MaxLength(500)]
        public string PreviewImage { get; set; }

        [Column("StoreId")]
        public long StoreId { get; set; }

        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; } = true;

        [Column("SyncDate")]
        [Required]
        public DateTime SyncDate { get; set; } = DateTime.UtcNow;

        [Column("CreatedDate")]
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("StoreId")]
        public virtual StoreMaster Store { get; set; }
    }
}
