using Financial_management_backend.Models;

namespace Financial_management_backend.Services.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string AdmissionNumber { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateOnly Birthdate { get; set; }
        public string GradeName { get; set; }
        public string? ParentName { get; set; }
        public string ParentFirstName { get; set; }
        public string ParentLastName { get; set; }
        public string? ParentPhoneNumber { get; set; }
        public StudentStatus Status { get; set; }
        public string EnrollmentTerm { get; set; }
        public int EnrollmentYear { get; set; }
    }
}
