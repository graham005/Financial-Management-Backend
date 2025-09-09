using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class ItemReceived
    {
        // Primary Key (GUID)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        // Reference to RequiredItem
        [Required]
        public Guid RequiredItemId { get; set; }
        [ForeignKey("RequiredItemId")]
        public RequiredItem RequiredItem { get; set; }

        // Foreign Key to Student (optional, if not already in RequiredItem)
        public Guid StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        // Quantity Received (can be less/more than expected)
        [Required]
        public decimal Quantity { get; set; }

        // Date Received
        [Required]
        public DateTime DateReceived { get; set; }

        // Recorded By (User who recorded the item)
        [Required]
        [StringLength(100)]
        public Guid RecordedBy { get; set; }
        
        // New properties
        [Required]
        [StringLength(20)]
        public string Term { get; set; }
        
        // Year for the academic term
        [Required]
        public int Year { get; set; }
        
        // Flag to indicate if money was received instead of items
        [Required]
        public bool IsMonetaryContribution { get; set; }
        
        // Value at time of contribution (in case RequiredItem.ApproximateValue changes later)
        public decimal ValueAtTimeOfContribution { get; set; }

        // Convenience properties for reporting (optional, not mapped)
        [NotMapped]
        public string ItemName => RequiredItem.ItemName;
        [NotMapped]
        public string Unit => RequiredItem.Unit;
    }
}
