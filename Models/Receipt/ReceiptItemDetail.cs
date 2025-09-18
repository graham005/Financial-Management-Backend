using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financial_management_backend.Models.Receipt
{
    public class ReceiptItemDetail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReceiptDataId { get; set; }

        [ForeignKey("ReceiptDataId")]
        public ReceiptData ReceiptData { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [StringLength(20)]
        public string Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }

        [StringLength(20)]
        public string ItemType { get; set; } // "Item" or "Money"

        [StringLength(500)]
        public string Notes { get; set; }
    }
}