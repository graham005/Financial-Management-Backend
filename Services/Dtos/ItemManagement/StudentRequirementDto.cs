namespace Financial_management_backend.Services.Dtos.ItemManagement
{
    public class StudentRequirementDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public Guid RequirementListId { get; set; }
        public string Term { get; set; }
        public int AcademicYear { get; set; }
        public string Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public List<RequirementStatusDto> RequirementItems { get; set; }
    }

    public class RequirementStatusDto
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal OutstandingQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MonetaryEquivalent { get; set; }
        public bool IsFulfilled { get; set; }
    }

    public class AssignRequirementDto
    {
        public Guid StudentId { get; set; }
        public Guid RequirementListId { get; set; }
    }
}