using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateRequiredItemDto
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string ItemName { get; set; }

        [Required(ErrorMessage = "Expected quantity is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Expected quantity must be greater than 0")]
        public decimal ExpectedQuantity { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; }
        
        [Required(ErrorMessage = "Approximate value is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Approximate value must be greater than 0")]
        public decimal ApproximateValue { get; set; }
    }

    public class UpdateRequiredItemDto
    {
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string ItemName { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Expected quantity must be greater than 0")]
        public decimal? ExpectedQuantity { get; set; }

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Approximate value must be greater than 0")]
        public decimal? ApproximateValue { get; set; }
    }

    public class RequiredItemDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public string Unit { get; set; }
        public decimal ApproximateValue { get; set; }
    }
}