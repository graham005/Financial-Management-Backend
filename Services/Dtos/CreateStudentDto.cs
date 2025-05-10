using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateStudentDto
    {
        [Required]
        public string AdmissionNumber { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime Birthdate { get; set; }
        [Required]
        public string GradeName { get; set; }
      
        public string ParentName { get; set; }       
        public string ParentPhoneNumber { get; set; }
    }
}
