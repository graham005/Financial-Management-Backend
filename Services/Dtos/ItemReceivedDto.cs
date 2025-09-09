using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateItemReceivedDto
    {
        [Required(ErrorMessage = "Required item ID is required")]
        public Guid RequiredItemId { get; set; }

        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Date received is required")]
        public DateTime DateReceived { get; set; }
        
        [Required(ErrorMessage = "Term is required")]
        [StringLength(20, ErrorMessage = "Term cannot exceed 20 characters")]
        public string Term { get; set; }
        
        [Required(ErrorMessage = "Year is required")]
        public int Year { get; set; }
        
        [Required(ErrorMessage = "Contribution type must be specified")]
        public bool IsMonetaryContribution { get; set; }
    }

    public class UpdateItemReceivedDto
    {
        public Guid? RequiredItemId { get; set; }
        public Guid StudentId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal? Quantity { get; set; }

        public DateTime? DateReceived { get; set; }
        
        [StringLength(20, ErrorMessage = "Term cannot exceed 20 characters")]
        public string Term { get; set; }
        
        public int? Year { get; set; }
        
        public bool? IsMonetaryContribution { get; set; }
    }

    public class ItemReceivedDto
    {
        public Guid Id { get; set; }
        public Guid RequiredItemId { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public Guid? StudentId { get; set; }
        public string StudentName { get; set; }
        public decimal Quantity { get; set; }
        public DateTime DateReceived { get; set; }
        public Guid RecordedBy { get; set; }
        public decimal VarianceQuantity { get; set; } // Difference between expected and received
        public string Term { get; set; }
        public int Year { get; set; }
        public bool IsMonetaryContribution { get; set; }
        public decimal ValueAtTimeOfContribution { get; set; }
        public decimal ApproximateValue { get; set; }
    }
}