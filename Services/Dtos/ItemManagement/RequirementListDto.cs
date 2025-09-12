namespace Financial_management_backend.Services.Dtos.ItemManagement
{
    public class CreateRequirementListDto
    {
        public string Term { get; set; }
        public int AcademicYear { get; set; }
    }

    public class CreateRequirementItemDto
    {
        public string ItemName { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public string Description { get; set; }
    }

    public class RequirementListDetailDto
    {
        public Guid Id { get; set; }
        public string Term { get; set; }
        public int AcademicYear { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        public List<RequirementItemDto> Items { get; set; }
    }

    public class RequirementItemDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public string Description { get; set; }
    }
}