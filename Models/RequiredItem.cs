using System;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Models
{
    public class RequiredItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; }

        [Required]
        public decimal ExpectedQuantity { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; }
    }
}