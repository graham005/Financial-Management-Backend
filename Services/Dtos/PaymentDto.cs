namespace Financial_management_backend.Services.Dtos
{
    public class PaymentDto
    {
        public Guid StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Term { get; set; }
        public List<FeeAllocationDto> FeeAllocations { get; set; }
    }

    public class FeeAllocationDto
    {
        public Guid FeeId { get; set; }
        public string FeeType { get; set; }
        public decimal Amount { get; set; }
    }
}
