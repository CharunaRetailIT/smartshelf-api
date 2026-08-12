namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class ComboDto
    {

        public class CreateComboRequest
        {
            public int DeviceId { get; set; }
            public string TemplateId { get; set; }
            public bool IsDefault { get; set; }
        }
    }
}
