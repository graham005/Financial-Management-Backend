using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateOtherFeeDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string GradeName { get; set; } 
        [Required]
        public decimal Amount { get; set; }
    }

    public class UpdateOtherFeeDto
    {
        public string? Name { get; set; }
        public string? GradeName { get; set; } // Changed from Guid? GradeId
        public decimal? Amount { get; set; }
    }
}
