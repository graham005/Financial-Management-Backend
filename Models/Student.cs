namespace Financial_management_backend.Models
{
    public class Student
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AdmissionNumber { get; set; }
        public string Name { get; set; }
        public DateTime Birthdate { get; set; }

        public int GradeId { get; set; }
        public Grade Grade { get; set; }

        public Guid ParentId { get; set; }
        public Parent Parent { get; set; }
    }
}
