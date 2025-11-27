using Financial_management_backend.Data;
using Financial_management_backend.Services.Dtos.Reports;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Financial_management_backend.Services.Reports
{
    public interface IReportService
    {
        Task<DailyCollectionsReportDto> GenerateDailyCollectionsReportAsync(ReportRequestDto request);
        Task<RevenueSummaryReportDto> GenerateRevenueSummaryReportAsync(ReportRequestDto request);
        Task<OutstandingFeesReportDto> GenerateOutstandingFeesReportAsync(ReportRequestDto request);
        Task<FeeCollectionRateReportDto> GenerateFeeCollectionRateReportAsync(ReportRequestDto request);
        Task<PaymentHistoryReportDto> GeneratePaymentHistoryReportAsync(ReportRequestDto request);
        Task<ItemTransactionReportDto> GenerateItemTransactionReportAsync(ReportRequestDto request);
        Task<StudentAccountStatementDto> GenerateStudentAccountStatementAsync(Guid studentId);
        Task LogReportGenerationAsync(string reportType, Guid userId, string parameters, string format, int recordCount);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly FeeService _feeService;
        private readonly IAcademicTermService _academicTermService;

        public ReportService(
            ApplicationDbContext context,
            FeeService feeService,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _feeService = feeService;
            _academicTermService = academicTermService;
        }

        public async Task<DailyCollectionsReportDto> GenerateDailyCollectionsReportAsync(ReportRequestDto request)
        {
            var reportDate = request.StartDate ?? DateTime.Today;

            var query = _context.Payments
                .Include(p => p.Student)
                .Include(p => p.FeePayments)
                .Where(p => p.PaymentDate.Date == reportDate.Date && p.Status == "Completed");

            // Apply filters
            if (request.GradeId.HasValue)
                query = query.Where(p => p.Student.GradeId == request.GradeId.Value);

            if (!string.IsNullOrEmpty(request.PaymentMethod))
                query = query.Where(p => p.PaymentMethod == request.PaymentMethod);

            var payments = await query.ToListAsync();

            // Breakdown by payment method
            var byPaymentMethod = payments
                .GroupBy(p => p.PaymentMethod ?? "Unknown")
                .Select(g => new PaymentMethodBreakdown
                {
                    PaymentMethod = g.Key,
                    Amount = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToList();

            // Breakdown by fee type
            var byFeeType = payments
                .SelectMany(p => p.FeePayments.Select(fp => new { fp.FeeType, fp.Amount }))
                .GroupBy(x => x.FeeType)
                .Select(g => new FeeTypeBreakdown
                {
                    FeeType = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .ToList();

            // Transaction list
            var transactions = payments.Select(p => new TransactionSummary
            {
                PaymentDate = p.PaymentDate,
                StudentName = p.Student.Name,
                AdmissionNumber = p.Student.AdmissionNumber,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod ?? "Unknown",
                Term = p.FeePayments.FirstOrDefault()?.Term ?? "N/A",
                Year = p.FeePayments.FirstOrDefault()?.Year ?? DateTime.Now.Year
            }).ToList();

            return new DailyCollectionsReportDto
            {
                ReportDate = reportDate,
                TotalCollected = payments.Sum(p => p.Amount),
                TransactionCount = payments.Count,
                ByPaymentMethod = byPaymentMethod,
                ByFeeType = byFeeType,
                Transactions = transactions
            };
        }

        public async Task<RevenueSummaryReportDto> GenerateRevenueSummaryReportAsync(ReportRequestDto request)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var term = request.Term ?? currentTerm;
            var year = request.Year ?? currentYear;

            // Get date range for the specified term/year
            var (startDate, endDate) = GetTermDateRange(term, year);

            var query = _context.Payments
                .Include(p => p.Student)
                    .ThenInclude(s => s.Grade)
                .Include(p => p.FeePayments)
                .Where(p => p.Status == "Completed" && 
                           p.PaymentDate >= startDate && 
                           p.PaymentDate <= endDate);

            var payments = await query.ToListAsync();

            // Revenue by fee type
            var byFeeType = payments
                .SelectMany(p => p.FeePayments)
                .GroupBy(fp => fp.FeeType)
                .Select(g => new RevenueByFeeType
                {
                    FeeType = g.Key,
                    Amount = g.Sum(fp => fp.Amount),
                    Percentage = 0 // Will calculate after
                })
                .ToList();

            var totalRevenue = byFeeType.Sum(x => x.Amount);
            foreach (var item in byFeeType)
            {
                item.Percentage = totalRevenue > 0 ? (item.Amount / totalRevenue) * 100 : 0;
            }

            // Revenue by grade
            var byGrade = payments
                .GroupBy(p => p.Student.Grade)
                .Select(g => new RevenueByGrade
                {
                    GradeName = g.Key.Name,
                    Amount = g.Sum(p => p.Amount),
                    StudentCount = g.Select(p => p.StudentId).Distinct().Count(),
                    AveragePerStudent = g.Sum(p => p.Amount) / g.Select(p => p.StudentId).Distinct().Count()
                })
                .OrderBy(x => x.GradeName)
                .ToList();

            // Monthly breakdown
            var monthlyBreakdown = payments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new MonthlyRevenue
                {
                    Month = g.Key,
                    MonthName = new DateTime(year, g.Key, 1).ToString("MMMM"),
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return new RevenueSummaryReportDto
            {
                Term = term,
                Year = year,
                TotalRevenue = totalRevenue,
                ByFeeType = byFeeType,
                ByGrade = byGrade,
                MonthlyBreakdown = monthlyBreakdown
            };
        }

        public async Task<OutstandingFeesReportDto> GenerateOutstandingFeesReportAsync(ReportRequestDto request)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var term = request.Term ?? currentTerm;
            var year = request.Year ?? currentYear;

            var students = await _context.Students
                .Include(s => s.Grade)
                .Where(s => !request.GradeId.HasValue || s.GradeId == request.GradeId.Value)
                .ToListAsync();

            var studentArrears = new List<StudentArrearsSummary>();

            foreach (var student in students)
            {
                var arrears = await _feeService.CalculateCumulativeArrears(student.Id, term, year);

                if (arrears > 0)
                {
                    // Find oldest unpaid term
                    var (oldestTerm, oldestYear) = await FindOldestUnpaidTerm(student.Id, term, year);

                    studentArrears.Add(new StudentArrearsSummary
                    {
                        AdmissionNumber = student.AdmissionNumber,
                        StudentName = student.Name,
                        Grade = student.Grade.Name,
                        OutstandingAmount = arrears,
                        OldestUnpaidTerm = oldestTerm,
                        OldestUnpaidYear = oldestYear
                    });
                }
            }

            // Group by grade
            var byGrade = studentArrears
                .GroupBy(s => s.Grade)
                .Select(g => new GradeArrearsSummary
                {
                    GradeName = g.Key,
                    StudentsWithArrears = g.Count(),
                    TotalOutstanding = g.Sum(s => s.OutstandingAmount),
                    AverageArrears = g.Average(s => s.OutstandingAmount)
                })
                .OrderBy(x => x.GradeName)
                .ToList();

            return new OutstandingFeesReportDto
            {
                Term = term,
                Year = year,
                TotalOutstanding = studentArrears.Sum(s => s.OutstandingAmount),
                StudentsWithArrears = studentArrears.Count,
                Students = studentArrears.OrderByDescending(s => s.OutstandingAmount).ToList(),
                ByGrade = byGrade
            };
        }

        public async Task<FeeCollectionRateReportDto> GenerateFeeCollectionRateReportAsync(ReportRequestDto request)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var term = request.Term ?? currentTerm;
            var year = request.Year ?? currentYear;

            var grades = await _context.Grades.ToListAsync();
            var gradeCollectionRates = new List<GradeCollectionRate>();

            decimal totalExpected = 0;
            decimal totalCollected = 0;

            foreach (var grade in grades)
            {
                if (request.GradeId.HasValue && grade.Id != request.GradeId.Value)
                    continue;

                var students = await _context.Students
                    .Where(s => s.GradeId == grade.Id)
                    .ToListAsync();

                if (!students.Any())
                    continue;

                // Calculate expected fees for this grade/term
                var feeStructure = await _context.FeeStructures
                    .FirstOrDefaultAsync(fs => fs.GradeId == grade.Id);

                if (feeStructure == null)
                    continue;

                decimal termFee = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };

                var expectedForGrade = termFee * students.Count;

                // Calculate collected fees
                var collectedForGrade = await _context.FeePayments
                    .Where(fp => fp.Term == term && 
                               fp.Year == year &&
                               fp.FeeType == "Tuition" &&
                               students.Select(s => s.Id).Contains(fp.Payment.StudentId) &&
                               fp.Payment.Status == "Completed")
                    .SumAsync(fp => fp.Amount);

                var collectionRate = expectedForGrade > 0 ? (collectedForGrade / expectedForGrade) * 100 : 0;

                gradeCollectionRates.Add(new GradeCollectionRate
                {
                    GradeName = grade.Name,
                    StudentCount = students.Count,
                    ExpectedFees = expectedForGrade,
                    CollectedFees = collectedForGrade,
                    CollectionRate = collectionRate
                });

                totalExpected += expectedForGrade;
                totalCollected += collectedForGrade;
            }

            return new FeeCollectionRateReportDto
            {
                Term = term,
                Year = year,
                ExpectedFees = totalExpected,
                CollectedFees = totalCollected,
                CollectionRate = totalExpected > 0 ? (totalCollected / totalExpected) * 100 : 0,
                OutstandingFees = totalExpected - totalCollected,
                ByGrade = gradeCollectionRates
            };
        }

        public async Task<PaymentHistoryReportDto> GeneratePaymentHistoryReportAsync(ReportRequestDto request)
        {
            var startDate = request.StartDate ?? DateTime.Today.AddMonths(-1);
            var endDate = request.EndDate ?? DateTime.Today;

            var query = _context.Payments
                .Include(p => p.Student)
                    .ThenInclude(s => s.Grade)
                .Include(p => p.FeePayments)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed");

            // Apply filters
            if (request.StudentId.HasValue)
                query = query.Where(p => p.StudentId == request.StudentId.Value);

            if (request.GradeId.HasValue)
                query = query.Where(p => p.Student.GradeId == request.GradeId.Value);

            if (!string.IsNullOrEmpty(request.PaymentMethod))
                query = query.Where(p => p.PaymentMethod == request.PaymentMethod);

            if (!string.IsNullOrEmpty(request.Term))
                query = query.Where(p => p.FeePayments.Any(fp => fp.Term == request.Term));

            if (request.Year.HasValue)
                query = query.Where(p => p.FeePayments.Any(fp => fp.Year == request.Year.Value));

            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

            // Get financial transactions to get receipt numbers
            var paymentIds = payments.Select(p => p.Id).ToList();
            var transactions = await _context.FinancialTransactions
                .Where(t => t.PaymentId.HasValue && paymentIds.Contains(t.PaymentId.Value))
                .Select(t => new { t.PaymentId, t.Id })
                .ToListAsync();

            // Get user names for "Processed By"
            var createdByIds = payments.Select(p => p.CreatedBy).Distinct().ToList();
            var users = await _context.Users
                .Where(u => createdByIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

            var paymentRecords = payments.Select(p => new PaymentRecord
            {
                PaymentDate = p.PaymentDate,
                ReceiptNumber = GenerateReceiptNumber(transactions.FirstOrDefault(t => t.PaymentId == p.Id)?.Id),
                StudentName = p.Student.Name,
                AdmissionNumber = p.Student.AdmissionNumber,
                Grade = p.Student.Grade.Name,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod ?? "Unknown",
                FeeType = string.Join(", ", p.FeePayments.Select(fp => fp.FeeType).Distinct()),
                Term = p.FeePayments.FirstOrDefault()?.Term ?? "N/A",
                Year = p.FeePayments.FirstOrDefault()?.Year ?? 0,
                ProcessedBy = users.ContainsKey(p.CreatedBy) ? users[p.CreatedBy] : "Unknown"
            }).ToList();

            return new PaymentHistoryReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalPayments = payments.Count,
                TotalAmount = payments.Sum(p => p.Amount),
                Payments = paymentRecords
            };
        }

        public async Task<ItemTransactionReportDto> GenerateItemTransactionReportAsync(ReportRequestDto request)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var term = request.Term ?? currentTerm;
            var year = request.Year ?? currentYear;

            // Get requirement list for this term/year
            var requirementList = await _context.RequirementLists
                .Include(rl => rl.Items)
                .FirstOrDefaultAsync(rl => rl.Term == term && rl.AcademicYear == year);

            if (requirementList == null)
            {
                return new ItemTransactionReportDto
                {
                    Term = term,
                    Year = year,
                    Items = new List<ItemFulfillmentSummary>(),
                    Students = new List<StudentItemStatus>()
                };
            }

            // Item fulfillment summary
            var itemSummaries = new List<ItemFulfillmentSummary>();

            foreach (var item in requirementList.Items)
            {
                var itemTransactions = await _context.ItemTransactions
                    .Where(it => it.RequirementItemId == item.Id && it.TransactionType == "Item")
                    .SumAsync(it => it.ItemQuantity ?? 0);

                var moneyContributions = await _context.ItemTransactions
                    .Where(it => it.RequirementItemId == item.Id && it.TransactionType == "Money")
                    .SumAsync(it => it.MoneyAmount ?? 0);

                var totalStudents = await _context.StudentRequirements
                    .Where(sr => sr.RequirementListId == requirementList.Id)
                    .CountAsync();

                var totalRequired = item.RequiredQuantity * totalStudents;
                var fulfillmentRate = totalRequired > 0 ? (itemTransactions / totalRequired) * 100 : 0;

                itemSummaries.Add(new ItemFulfillmentSummary
                {
                    ItemName = item.ItemName,
                    RequiredQuantity = totalRequired,
                    ReceivedQuantity = itemTransactions,
                    MoneyContributed = moneyContributions,
                    FulfillmentRate = fulfillmentRate,
                    Unit = item.Unit
                });
            }

            // Student item status
            var studentRequirements = await _context.StudentRequirements
                .Include(sr => sr.Student)
                    .ThenInclude(s => s.Grade)
                .Include(sr => sr.Transactions)
                .Where(sr => sr.RequirementListId == requirementList.Id)
                .ToListAsync();

            var studentStatuses = studentRequirements.Select(sr => new StudentItemStatus
            {
                StudentName = sr.Student.Name,
                AdmissionNumber = sr.Student.AdmissionNumber,
                Grade = sr.Student.Grade.Name,
                Status = sr.Status,
                ItemsValue = sr.Transactions.Where(t => t.TransactionType == "Item")
                    .Sum(t => (t.ItemQuantity ?? 0) * (t.RequirementItem?.UnitPrice ?? 0)),
                MoneyContributed = sr.Transactions.Where(t => t.TransactionType == "Money")
                    .Sum(t => t.MoneyAmount ?? 0)
            }).ToList();

            return new ItemTransactionReportDto
            {
                Term = term,
                Year = year,
                Items = itemSummaries,
                Students = studentStatuses
            };
        }

        public async Task<StudentAccountStatementDto> GenerateStudentAccountStatementAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found");

            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            // Get all terms since enrollment
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            var termSummaries = new List<TermFeeSummary>();

            decimal totalCharged = 0;
            decimal totalPaid = 0;

            for (int year = student.EnrollmentYear; year <= currentYear; year++)
            {
                foreach (var term in terms)
                {
                    // Skip terms before enrollment
                    if (year == student.EnrollmentYear && 
                        Array.IndexOf(terms, term) < Array.IndexOf(terms, student.EnrollmentTerm))
                        continue;

                    // Skip future terms
                    if (year == currentYear && 
                        Array.IndexOf(terms, term) > Array.IndexOf(terms, currentTerm))
                        continue;

                    var (feesCharged, amountPaid) = await GetTermFeeDetails(studentId, term, year);

                    termSummaries.Add(new TermFeeSummary
                    {
                        Term = term,
                        Year = year,
                        FeesCharged = feesCharged,
                        AmountPaid = amountPaid,
                        Outstanding = feesCharged - amountPaid
                    });

                    totalCharged += feesCharged;
                    totalPaid += amountPaid;
                }
            }

            // Get payment history
            var payments = await _context.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var paymentIds = payments.Select(p => p.Id).ToList();
            var transactions = await _context.FinancialTransactions
                .Where(t => t.PaymentId.HasValue && paymentIds.Contains(t.PaymentId.Value))
                .Select(t => new { t.PaymentId, t.Id })
                .ToListAsync();

            var paymentHistory = payments.Select(p => new PaymentTransaction
            {
                PaymentDate = p.PaymentDate,
                ReceiptNumber = GenerateReceiptNumber(transactions.FirstOrDefault(t => t.PaymentId == p.Id)?.Id),
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod ?? "Unknown",
                Description = $"Payment via {p.PaymentMethod}"
            }).ToList();

            return new StudentAccountStatementDto
            {
                StudentName = student.Name,
                AdmissionNumber = student.AdmissionNumber,
                Grade = student.Grade.Name,
                EnrollmentTerm = student.EnrollmentTerm,
                EnrollmentYear = student.EnrollmentYear,
                TotalFeesCharged = totalCharged,
                TotalPaid = totalPaid,
                CurrentBalance = totalCharged - totalPaid,
                ByTerm = termSummaries,
                PaymentHistory = paymentHistory
            };
        }

        public async Task LogReportGenerationAsync(string reportType, Guid userId, string parameters, string format, int recordCount)
        {
            // This would log to a ReportAuditLog table if you create one
            // For now, we can log to a file or just skip
            await Task.CompletedTask;
        }

        // Helper methods
        private (DateTime startDate, DateTime endDate) GetTermDateRange(string term, int year)
        {
            return term switch
            {
                "Term 1" => (new DateTime(year, 1, 1), new DateTime(year, 4, 30)),
                "Term 2" => (new DateTime(year, 5, 1), new DateTime(year, 8, 31)),
                "Term 3" => (new DateTime(year, 9, 1), new DateTime(year, 12, 31)),
                _ => (new DateTime(year, 1, 1), new DateTime(year, 12, 31))
            };
        }

        private async Task<(string term, int year)> FindOldestUnpaidTerm(Guid studentId, string currentTerm, int currentYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return (currentTerm, currentYear);

            var terms = new[] { "Term 1", "Term 2", "Term 3" };

            for (int year = student.EnrollmentYear; year <= currentYear; year++)
            {
                foreach (var term in terms)
                {
                    if (year == student.EnrollmentYear && 
                        Array.IndexOf(terms, term) < Array.IndexOf(terms, student.EnrollmentTerm))
                        continue;

                    var (charged, paid) = await GetTermFeeDetails(studentId, term, year);
                    if (charged > paid)
                        return (term, year);
                }
            }

            return (currentTerm, currentYear);
        }

        private async Task<(decimal charged, decimal paid)> GetTermFeeDetails(Guid studentId, string term, int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return (0, 0);

            // Get grade for this term/year
            var gradeHistoryService = new StudentGradeHistoryService(_context, _academicTermService);
            Guid gradeId;
            try
            {
                gradeId = await gradeHistoryService.GetStudentGradeForTermAsync(studentId, term, year);
            }
            catch
            {
                return (0, 0);
            }

            // Get fee structure
            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.GradeId == gradeId);

            decimal charged = 0;
            if (feeStructure != null)
            {
                charged = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };
            }

            // Get paid amount
            var paid = await _context.FeePayments
                .Where(fp => fp.Payment.StudentId == studentId &&
                           fp.Term == term &&
                           fp.Year == year &&
                           fp.FeeType == "Tuition" &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => fp.Amount);

            return (charged, paid);
        }

        private string GenerateReceiptNumber(Guid? transactionId)
        {
            if (!transactionId.HasValue)
                return "N/A";

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var shortId = transactionId.Value.ToString()[..8];
            return $"RCP-{timestamp}-{shortId.ToUpper()}";
        }
    }
}