using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos.Reports
{
    // Base request for all reports
    public class ReportRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Term { get; set; }
        public int? Year { get; set; }
        public Guid? GradeId { get; set; }
        public Guid? StudentId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? FeeType { get; set; }
        public string Format { get; set; } = "PDF"; // PDF or Excel
    }

    // Daily Collections Report
    public class DailyCollectionsReportDto
    {
        public DateTime ReportDate { get; set; }
        public decimal TotalCollected { get; set; }
        public int TransactionCount { get; set; }
        public List<PaymentMethodBreakdown> ByPaymentMethod { get; set; }
        public List<FeeTypeBreakdown> ByFeeType { get; set; }
        public List<TransactionSummary> Transactions { get; set; }
    }

    public class PaymentMethodBreakdown
    {
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class FeeTypeBreakdown
    {
        public string FeeType { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class TransactionSummary
    {
        public DateTime PaymentDate { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
    }

    // Revenue Summary Report
    public class RevenueSummaryReportDto
    {
        public string Term { get; set; }
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RevenueByFeeType> ByFeeType { get; set; }
        public List<RevenueByGrade> ByGrade { get; set; }
        public List<MonthlyRevenue> MonthlyBreakdown { get; set; }
    }

    public class RevenueByFeeType
    {
        public string FeeType { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RevenueByGrade
    {
        public string GradeName { get; set; }
        public decimal Amount { get; set; }
        public int StudentCount { get; set; }
        public decimal AveragePerStudent { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal Amount { get; set; }
    }

    // Outstanding Fees Report
    public class OutstandingFeesReportDto
    {
        public string Term { get; set; }
        public int Year { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int StudentsWithArrears { get; set; }
        public List<StudentArrearsSummary> Students { get; set; }
        public List<GradeArrearsSummary> ByGrade { get; set; }
    }

    public class StudentArrearsSummary
    {
        public string AdmissionNumber { get; set; }
        public string StudentName { get; set; }
        public string Grade { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string OldestUnpaidTerm { get; set; }
        public int OldestUnpaidYear { get; set; }
    }

    public class GradeArrearsSummary
    {
        public string GradeName { get; set; }
        public int StudentsWithArrears { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal AverageArrears { get; set; }
    }

    // Fee Collection Rate Report
    public class FeeCollectionRateReportDto
    {
        public string Term { get; set; }
        public int Year { get; set; }
        public decimal ExpectedFees { get; set; }
        public decimal CollectedFees { get; set; }
        public decimal CollectionRate { get; set; } // Percentage
        public decimal OutstandingFees { get; set; }
        public List<GradeCollectionRate> ByGrade { get; set; }
    }

    public class GradeCollectionRate
    {
        public string GradeName { get; set; }
        public int StudentCount { get; set; }
        public decimal ExpectedFees { get; set; }
        public decimal CollectedFees { get; set; }
        public decimal CollectionRate { get; set; }
    }

    // Payment History Report
    public class PaymentHistoryReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentRecord> Payments { get; set; }
    }

    public class PaymentRecord
    {
        public DateTime PaymentDate { get; set; }
        public string ReceiptNumber { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public string Grade { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string FeeType { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
        public string ProcessedBy { get; set; }
    }

    // Item Transaction Report
    public class ItemTransactionReportDto
    {
        public string Term { get; set; }
        public int Year { get; set; }
        public List<ItemFulfillmentSummary> Items { get; set; }
        public List<StudentItemStatus> Students { get; set; }
    }

    public class ItemFulfillmentSummary
    {
        public string ItemName { get; set; }
        public decimal RequiredQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal MoneyContributed { get; set; }
        public decimal FulfillmentRate { get; set; }
        public string Unit { get; set; }
    }

    public class StudentItemStatus
    {
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public string Grade { get; set; }
        public string Status { get; set; } // Complete, Partial, Pending
        public decimal ItemsValue { get; set; }
        public decimal MoneyContributed { get; set; }
    }

    // Student Account Statement
    public class StudentAccountStatementDto
    {
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public string Grade { get; set; }
        public string EnrollmentTerm { get; set; }
        public int EnrollmentYear { get; set; }
        public decimal TotalFeesCharged { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal CurrentBalance { get; set; }
        public List<TermFeeSummary> ByTerm { get; set; }
        public List<PaymentTransaction> PaymentHistory { get; set; }
    }

    public class TermFeeSummary
    {
        public string Term { get; set; }
        public int Year { get; set; }
        public decimal FeesCharged { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class PaymentTransaction
    {
        public DateTime PaymentDate { get; set; }
        public string ReceiptNumber { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Description { get; set; }
    }

    // Report Audit Log
    public class ReportAuditLog
    {
        public Guid Id { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Guid GeneratedBy { get; set; }
        public string GeneratedByUsername { get; set; }
        public string Parameters { get; set; } // JSON
        public string Format { get; set; }
        public int RecordCount { get; set; }
    }
}