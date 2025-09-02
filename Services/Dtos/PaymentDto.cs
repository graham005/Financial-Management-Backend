namespace Financial_management_backend.Services.Dtos
{
    public class PaymentDto
    {
        public Guid StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public List<FeeAllocationDto> FeeAllocations { get; set; } = new();
    }

    public class FeeAllocationDto
    {
        public Guid FeeId { get; set; }
        public string FeeType { get; set; } // "Tuition", "Transport", "Custom", etc.
        public string FeeSource { get; set; } // "FeeStructure", "OtherFee", "CustomFee"
        public string Term { get; set; } // REQUIRED: "Term 1", "Term 2", "Term 3"
        public int Year { get; set; } // REQUIRED: 2023, 2024, etc.
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    // New DTO for getting available fees to pay
    public class AvailableFeesDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public List<AvailableFeeItemDto> AvailableFees { get; set; } = new();
        public decimal TotalOutstanding { get; set; }
    }

    public class AvailableFeeItemDto
    {
        public Guid FeeId { get; set; }
        public string FeeType { get; set; }
        public string FeeSource { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string Description { get; set; }
        public bool IsOverdue { get; set; }
    }
}
