namespace Financial_management_backend.Models
{
    public class FeeStructure
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int GradeId { get; set; }
        public Grade Grade { get; set; }

        public decimal Term1Fee { get; set; }
        public decimal Term2Fee { get; set; }
        public decimal Term3Fee { get; set; }
        public decimal TotalFee => Term1Fee + Term2Fee + Term3Fee;
    }
}
