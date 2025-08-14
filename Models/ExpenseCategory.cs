using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Models
{
    public class ExpenseCategory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BudgetAmount { get; set; }
    }
}