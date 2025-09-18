using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.Receipt
{
    public class ReceiptData
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [StringLength(20)]
        public string ReceiptType { get; set; } // "Payment" or "Item"

        [Required]
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid IssuedBy { get; set; }

        // Organization Details
        [Required]
        [StringLength(200)]
        public string OrganizationName { get; set; }

        [StringLength(500)]
        public string OrganizationAddress { get; set; }

        [StringLength(50)]
        public string OrganizationPhone { get; set; }

        [StringLength(100)]
        public string OrganizationEmail { get; set; }

        // Student/Recipient Details
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string StudentName { get; set; }

        [StringLength(50)]
        public string AdmissionNumber { get; set; }

        [StringLength(100)]
        public string Grade { get; set; }

        // Transaction Details
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Term { get; set; }

        public int Year { get; set; }

        // Staff Details
        [Required]
        [StringLength(200)]
        public string IssuedByName { get; set; }

        // Additional Notes
        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}