namespace Financial_management_backend.Services.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string AdmissionNumber { get; set; }
        public string Name { get; set; }
        public DateTime Birthdate { get; set; }
        public string GradeName { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhoneNumber { get; set; }
    }
}
