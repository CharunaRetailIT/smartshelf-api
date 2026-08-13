using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    /// <summary>
    /// One ESL label currently bound to a product, flattened for external API
    /// consumers so they don't have to walk DeviceAssignment -> combo -> device
    /// themselves. A device bound with both a template and a message appears
    /// once, with both sets of fields populated.
    /// </summary>
    public class ProductEslDto
    {
        public long DeviceId { get; set; }
        public string Mac { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }
        public string Status { get; set; }
        public int? Battery { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }

        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public long? TemplateAssignmentId { get; set; }
        public long? DeviceTemplateComboId { get; set; }

        public long? MessageId { get; set; }
        public string MessageName { get; set; }
        public long? MessageAssignmentId { get; set; }
        public long? DeviceMessageComboId { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// A product plus every ESL bound to it. Returned by the lookup endpoints
    /// (by id / by product code) so a single call answers "what is this product
    /// and which labels show it".
    /// </summary>
    public class ProductDetailDto
    {
        public long Id { get; set; }
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSyncToCloud { get; set; }
        public long StoreId { get; set; }
        public string StoreName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        /// <summary>True when at least one ESL is bound - lets a caller branch
        /// without inspecting the collection.</summary>
        public bool HasEsl => EslDevices != null && EslDevices.Count > 0;

        public List<ProductEslDto> EslDevices { get; set; } = new List<ProductEslDto>();
    }

    /// <summary>
    /// One ESL bound to a device, seen from the device side.
    /// </summary>
    public class DeviceBindingDto
    {
        public long AssignmentId { get; set; }
        public string AssignmentType { get; set; }   // TEMPLATE | MESSAGE
        public string LocationType { get; set; }     // Product | Shelf | Aisle
        public long LocationId { get; set; }

        public long? ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }

        public string TemplateId { get; set; }
        public string TemplateName { get; set; }

        public long? MessageId { get; set; }
        public string MessageName { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Full device record plus whatever it is currently bound to.
    /// </summary>
    public class DeviceDetailDto
    {
        public long Id { get; set; }
        public string Mac { get; set; }
        public string DeviceName { get; set; }
        public string Description { get; set; }
        public string DeviceType { get; set; }
        public string MinewDeviceId { get; set; }
        public string Status { get; set; }
        public int? Battery { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public string Firmware { get; set; }
        public string Hardware { get; set; }
        public string IPAddress { get; set; }
        public string NetworkName { get; set; }

        public long? ScreenId { get; set; }
        public decimal? ScreenInch { get; set; }
        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }
        public string ScreenColor { get; set; }

        public long StoreId { get; set; }
        public string StoreName { get; set; }
        public string MinewStoreId { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public bool IsBound => Bindings != null && Bindings.Count > 0;

        public List<DeviceBindingDto> Bindings { get; set; } = new List<DeviceBindingDto>();
    }

    #region Bulk

    /// <summary>
    /// One product in a bulk create payload. Categories may be given by id or
    /// by name - name lookup exists because an external POS rarely knows our
    /// internal category ids.
    /// </summary>
    public class BulkProductCreateItem
    {
        public string ProductCode { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }

        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }

        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Optional ESL bindings to create along with the product.</summary>
        public List<EslAssignmentIntent> EslAssignments { get; set; }
    }

    /// <summary>
    /// One product in a bulk update payload. The row is matched on ProductCode
    /// (within StoreId) unless an explicit Id is supplied. Every price/field is
    /// nullable so a caller can send a price-only update without having to
    /// re-send the whole product and risk blanking fields it doesn't own.
    /// </summary>
    public class BulkProductUpdateItem
    {
        public long? Id { get; set; }
        public string ProductCode { get; set; }

        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }

        public decimal? Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? WholesalePrice { get; set; }
        public decimal? MinimumPrice { get; set; }
        public decimal? MaximumPrice { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class BulkProductCreateRequest
    {
        public long StoreId { get; set; }
        public int UserId { get; set; }

        /// <summary>When false the ESL push is skipped entirely (data-only load).
        /// Default true: prices reach the labels without a second call.</summary>
        public bool PushToEsl { get; set; } = true;

        /// <summary>Bind newly-created ESL assignments in the vendor cloud so the
        /// labels start showing the product. Default on.</summary>
        public bool BindToEsl { get; set; } = true;

        public List<BulkProductCreateItem> Products { get; set; } = new List<BulkProductCreateItem>();
    }

    public class BulkProductUpdateRequest
    {
        public long StoreId { get; set; }
        public int UserId { get; set; }
        public bool PushToEsl { get; set; } = true;
        public List<BulkProductUpdateItem> Products { get; set; } = new List<BulkProductUpdateItem>();
    }

    /// <summary>
    /// Outcome of a single row. Rows are independent - one bad row does not
    /// abort the batch, it just reports Success=false with a reason.
    /// </summary>
    public class BulkRowResultDto
    {
        public int Index { get; set; }
        public string ProductCode { get; set; }
        public long? ProductId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        /// <summary>Null when no push was attempted (PushToEsl=false, row failed,
        /// or the store has no Minew store linked).</summary>
        public bool? EslPushed { get; set; }
        public string EslMessage { get; set; }

        /// <summary>Null when the row carried no ESL assignments, or BindToEsl was
        /// false. Binding is what makes the label actually display the product -
        /// distinct from EslPushed, which only updates the product data.</summary>
        public bool? EslBound { get; set; }
        public string EslBindMessage { get; set; }
    }

    public class BulkOperationResultDto
    {
        public long StoreId { get; set; }
        public int TotalRows { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int EslPushSucceeded { get; set; }
        public int EslPushFailed { get; set; }
        public int EslBoundSucceeded { get; set; }
        public int EslBoundFailed { get; set; }
        public string EslSummary { get; set; }
        public List<BulkRowResultDto> Results { get; set; } = new List<BulkRowResultDto>();
    }

    #endregion
}
