using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.ItemManagement
{
    /// <summary>
    /// Represents a snapshot of item requirements for a specific term/year.
    /// This is immutable once created.
    /// </summary>
    public class RequirementList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        [Required]
        public string Term { get; set; }
        
        [Required]
        public int AcademicYear { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public Guid CreatedBy { get; set; }
        
        [ForeignKey("CreatedBy")]
        public User Creator { get; set; }
        
        [Required]
        public string Status { get; set; } = "Active"; // Active, Archived
        
        // Navigation property
        public ICollection<RequirementItem> Items { get; set; } = new List<RequirementItem>();
    }
}