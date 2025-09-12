using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.ItemManagement
{
    /// <summary>
    /// Assigns a requirement list to a specific student, creating their obligation.
    /// </summary>
    public class StudentRequirement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        [Required]
        public Guid StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student Student { get; set; }
        
        [Required]
        public Guid RequirementListId { get; set; }
        
        [ForeignKey("RequirementListId")]
        public RequirementList RequirementList { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Partial, Complete, In Arrears
        
        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public Guid AssignedBy { get; set; }
        
        [ForeignKey("AssignedBy")]
        public User Assigner { get; set; }
        
        // Navigation property
        public ICollection<ItemTransaction> Transactions { get; set; } = new List<ItemTransaction>();
    }
}