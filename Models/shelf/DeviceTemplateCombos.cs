using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TERMS_LOYALTY_API.DTOs.shelf;

namespace TERMS_LOYALTY_API.Models.shelf
{
    public class DeviceTemplateCombos :BaseEntity
    {

        [Column("DeviceId")]
        [Required]
        [MaxLength(100)]
        public long DeviceId { get; set; }

        [Column("TemplateId")]
        [Required]
        [MaxLength(100)]
        public string TemplateId { get; set; }

        [Column("IsDefault")]
        [Required]
        public bool IsDefault { get; set; } = false;

        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; } = true;

        [Column("Priority")]
        [Required]
        public int Priority { get; set; } = 0;

        // Navigation properties
        [ForeignKey("DeviceId")]
        public virtual DeviceMaster Device { get; set; }



        [ForeignKey("TemplateId")]
        public virtual MinewTemplates Template { get; set; }

        public virtual ICollection<DeviceTemplateAssignment> Assignments { get; set; }
    }
}
