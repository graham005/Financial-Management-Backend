using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateFeeStructureDto
    {
        [Required]
        public string GradeName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Term1Fee { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Term2Fee { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Term3Fee { get; set; }
    }
}
