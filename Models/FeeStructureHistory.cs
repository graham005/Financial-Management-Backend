using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class FeeStructureHistory
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid GradeId { get; set; }
        [ForeignKey("GradeId")]
        public Grade Grade { get; set; }
        
        [Required]
        public decimal Term1Fee { get; set; }
        
        [Required]
        public decimal Term2Fee { get; set; }
        
        [Required]
        public decimal Term3Fee { get; set; }
        
        [NotMapped]
        public decimal TotalFee => Term1Fee + Term2Fee + Term3Fee;
        
        // Academic year this structure applies to (e.g., 2023)
        [Required]
        public int AcademicYear { get; set; }
        
        [Required]
        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}