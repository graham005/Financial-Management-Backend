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
        [StringLength(50)]
        public string FeeType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
