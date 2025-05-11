namespace Financial_management_backend.Models
{
    public class Student
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AdmissionNumber { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateOnly Birthdate { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }

        public Guid ParentId { get; set; }
        public Parent Parent { get; set; }
    }
}
