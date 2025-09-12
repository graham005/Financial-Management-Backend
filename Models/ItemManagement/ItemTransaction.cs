using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.ItemManagement
{
    /// <summary>
    /// Records items or money received from students.
    /// </summary>
    public class ItemTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        [Required]
        public Guid StudentRequirementId { get; set; }
        
        [ForeignKey("StudentRequirementId")]
        public StudentRequirement StudentRequirement { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; }
        
        [Required]
        public string TransactionType { get; set; } // "Item", "Money", "Adjustment"
        
        // If transaction type is "Item"
        public Guid? RequirementItemId { get; set; }
        
        [ForeignKey("RequirementItemId")]
        public RequirementItem RequirementItem { get; set; }
        
        public decimal? ItemQuantity { get; set; }
        
        // If transaction type is "Money"
        public decimal? MoneyAmount { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        [Required]
        public Guid RecordedBy { get; set; }
        
        [ForeignKey("RecordedBy")]
        public User Recorder { get; set; }
    }
}