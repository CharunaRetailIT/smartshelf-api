namespace TERMS_LOYALTY_API.Models.shelf
{
    public class ContentType: BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
