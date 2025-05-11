namespace Financial_management_backend.Services.Dtos
{
    public class FeeStructureDto
    {
        public Guid Id { get; set; }
        public string GradeName { get; set; }
        public decimal Term1Fee { get; set; }
        public decimal Term2Fee { get; set; }
        public decimal Term3Fee { get; set; }
        public decimal TotalFee { get; set; }
    }
}
