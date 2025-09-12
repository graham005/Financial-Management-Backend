namespace Financial_management_backend.Services.Dtos.ItemManagement
{
    public class RecordTransactionDto
    {
        public Guid StudentRequirementId { get; set; }
        public DateTime TransactionDate { get; set; }
        public List<TransactionItemDto> Items { get; set; }
    }

    public class TransactionItemDto
    {
        public string TransactionType { get; set; } // "Item" or "Money"
        public Guid? RequirementItemId { get; set; }
        public decimal? ItemQuantity { get; set; }
        public decimal? MoneyAmount { get; set; }
        public string Notes { get; set; }
    }

    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid StudentRequirementId { get; set; }
        public string StudentName { get; set; }
        public string Term { get; set; }
        public int AcademicYear { get; set; }
        public DateTime TransactionDate { get; set; }
        public List<TransactionDetailDto> Items { get; set; }
        public bool RequirementFulfilled { get; set; }
    }

    public class TransactionDetailDto
    {
        public Guid Id { get; set; }
        public string TransactionType { get; set; }
        public string ItemName { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? MoneyAmount { get; set; }
        public string Notes { get; set; }
    }
}