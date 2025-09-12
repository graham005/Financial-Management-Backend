using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.ItemManagement
{
    /// <summary>
    /// Represents a specific item within a requirement list.
    /// </summary>
    public class RequirementItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        [Required]
        public Guid RequirementListId { get; set; }
        
        [ForeignKey("RequirementListId")]
        public RequirementList RequirementList { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ItemName { get; set; }
        
        [Required]
        public decimal RequiredQuantity { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Unit { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
    }
}