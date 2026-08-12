using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DeviceTemplateDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public Guid TemplateId { get; set; }
        public string DeviceName { get; set; }
        public string TemplateName { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public decimal? ScreenInch { get; set; }
        public string Battery { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public class CreateComboRequest
        {
            public string DeviceId { get; set; }
            public string TemplateId { get; set; }
            public bool IsDefault { get; set; }
        }

        public class CreateAssignmentRequest
        {
            public long DeviceTemplateComboId { get; set; }
            public string LocationType { get; set; }
            public long LocationId { get; set; }
            public int UserId { get; set; }
        }

        public class BindDataRequest
        {
            public long ComboId { get; set; }
            public string StoreId { get; set; }
            public Dictionary<string, string> GoodsMap { get; set; }
            public int? Color { get; set; }
            public int? Total { get; set; }
            public int? Period { get; set; }
            public int? Interval { get; set; }
            public int? Brightness { get; set; }
        }

        public class QuickBindRequest
        {
            public long ProductId { get; set; }
            public long ComboId { get; set; }
        }
    }
}
