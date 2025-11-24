using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models
{
    public class FeePayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public Payment Payment { get; set; }

        [Required]
        public Guid FeeId { get; set; }

        [Required]
        [StringLength(40)]
        public string FeeSource { get; set; }

        [Required]
        [StringLength(50)]
        public string FeeType { get; set; }

        [Required]
        public Guid GradeId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Term { get; set; } // "Term 1", "Term 2", "Term 3"

        [Required]
        public int Year { get; set; } // 2023, 2024, etc.
    }
}
