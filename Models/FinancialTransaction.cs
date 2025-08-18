using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class FinancialTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // e.g. "FeePayment", "Expense", "Income"

        [StringLength(50)]
        public string Category { get; set; } // e.g. "Tuition", "Transport", "Salary", etc.

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        // Optional: Reference to payment if applicable
        public Guid? PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public Payment Payment { get; set; }

        // Optional: Link to Expense if this transaction is an expense
        public Guid? ExpenseId { get; set; }
        [ForeignKey("ExpenseId")]
        public Expense Expense { get; set; }

        // Status (e.g., Pending, Completed, Cancelled)
        [StringLength(30)]
        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}