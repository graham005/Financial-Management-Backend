using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class ExpenseCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal BudgetAmount { get; set; }
    }

    public class CreateExpenseCategoryDto
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public decimal BudgetAmount { get; set; }
    }

    public class UpdateExpenseCategoryDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? BudgetAmount { get; set; }
    }
}