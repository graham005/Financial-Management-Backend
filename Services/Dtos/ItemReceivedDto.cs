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
    }

    public class UpdateItemReceivedDto
    {
        public Guid? RequiredItemId { get; set; }
        public Guid StudentId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal? Quantity { get; set; }

        public DateTime? DateReceived { get; set; }
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
    }
}