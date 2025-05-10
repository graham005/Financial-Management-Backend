using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateGradeDto
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; }
    }
}
