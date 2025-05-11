using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    public class CreateStudentDto
    {
        [Required]
        public string AdmissionNumber { get; set; }
        [Required]
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        [Required]
        public DateOnly Birthdate { get; set; }
        [Required]
        public string GradeName { get; set; }
      
        public string ParentName { get; set; }
        public string ParentFirstName { get; set; }
        public string ParentLastName { get; set; }
        public string ParentPhoneNumber { get; set; }
    }
}
