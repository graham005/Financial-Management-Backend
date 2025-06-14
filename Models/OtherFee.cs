using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class OtherFee
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public int GradeId { get; set; }
        [ForeignKey("GradeId")]
        public Grade Grade { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
