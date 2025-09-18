namespace Financial_management_backend.Services.Dtos
{
    public class ReceiptDataDto
    {
        public string ReceiptNumber { get; set; }
        public string ReceiptType { get; set; } // "Payment" or "Item"
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        
        // Organization Details
        public OrganizationDetailsDto Organization { get; set; }
        
        // Student Details
        public StudentDetailsDto Student { get; set; }
        
        // Transaction Details
        public TransactionDetailsDto Transaction { get; set; }
        
        // Staff Details
        public StaffDetailsDto IssuedBy { get; set; }
        
        // Items for receipt
        public List<ReceiptItemDto> Items { get; set; } = new();
        
        public string Notes { get; set; }
    }

    public class OrganizationDetailsDto
    {
        public string Name { get; set; } = "Your School Name"; // You can configure this
        public string Address { get; set; } = "School Address";
        public string Phone { get; set; } = "School Phone";
        public string Email { get; set; } = "School Email";
        public string Logo { get; set; } // Base64 or URL for logo
    }

    public class StudentDetailsDto
    {
        public Guid StudentId { get; set; }
        public string Name { get; set; }
        public string AdmissionNumber { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public string ParentContact { get; set; }
    }

    public class TransactionDetailsDto
    {
        public Guid TransactionId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string Description { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
    }

    public class StaffDetailsDto
    {
        public string Name { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
    }

    public class ReceiptItemDto
    {
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string ItemType { get; set; } // "Fee", "Item", "Money"
        public string Description { get; set; }
    }
}