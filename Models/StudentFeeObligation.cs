using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class StudentFeeObligation
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Term { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FeeType { get; set; } // "Tuition", "Books", etc.
        
        [Required]
        public Guid FeeSourceId { get; set; } // Reference to FeeStructureHistory, CustomFee, etc.
        
        [Required]
        [StringLength(50)]
        public string FeeSourceType { get; set; } // "FeeStructureHistory", "CustomFee", "OtherFee"
        
        [Required]
        public decimal AmountCharged { get; set; } // The original amount charged
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        // Denormalized for easier querying
        public decimal AmountPaid { get; set; }
        
        [NotMapped]
        public decimal OutstandingAmount => Math.Max(AmountCharged - AmountPaid, 0);
    }
}