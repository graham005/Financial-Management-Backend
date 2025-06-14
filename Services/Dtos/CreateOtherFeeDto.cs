using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateOtherFeeDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public int GradeId { get; set; }
        [Required]
        public decimal Amount { get; set; }
    }

    public class UpdateOtherFeeDto
    {
        public string? Name { get; set; }
        public int? GradeId { get; set; }
        public decimal? Amount { get; set; }
    }
}
