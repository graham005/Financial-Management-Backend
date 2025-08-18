using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public Guid ExpenseCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Vendor { get; set; }
        public string Description { get; set; }
        public string ApprovalStatus { get; set; }
        public Guid? CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseDto
    {
        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public Guid ExpenseCategoryId { get; set; }
        public string Vendor { get; set; }
        public string Description { get; set; }
    }

    public class UpdateExpenseDto
    {
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }
        public Guid? ExpenseCategoryId { get; set; }
        public string Vendor { get; set; }
        public string Description { get; set; }
        public string ApprovalStatus { get; set; }
    }
}