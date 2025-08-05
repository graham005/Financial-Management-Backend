namespace Financial_management_backend.Services.Dtos
{
    public class ArrearsResponseDto
    {
        public string StudentName { get; set; }
        public string EnrollementTerm { get; set; }
        public int EnrollmentYear { get; set; }
        public decimal CumulativeArrears { get; set; }
    }
}
