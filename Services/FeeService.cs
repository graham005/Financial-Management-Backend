using Financial_management_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public class FeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStudentGradeHistoryService _gradeHistoryService;
        private readonly IHistoricalFeeStructureService _historicalFeeService;

        public FeeService(
            ApplicationDbContext context,
            IStudentGradeHistoryService gradeHistoryService,
            IHistoricalFeeStructureService historicalFeeService)
        {
            _context = context;
            _gradeHistoryService = gradeHistoryService;
            _historicalFeeService = historicalFeeService;
        }

        /// <summary>
        /// Calculates outstanding fees for a student in a specific term/year.
        /// Uses historical grade and fee structure data.
        /// </summary>
        public async Task<decimal> CalculateOutstandingFees(Guid studentId, string term, int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 0;

            // CRITICAL FIX: Get the grade the student was ACTUALLY in during this term/year
            Guid historicalGradeId;
            try
            {
                historicalGradeId = await _gradeHistoryService.GetStudentGradeForTermAsync(studentId, term, year);
            }
            catch
            {
                // If student wasn't enrolled yet, return 0
                return 0;
            }

            // First check for fee obligations (if they exist)
            var obligations = await _context.StudentFeeObligations
                .Where(o => o.StudentId == studentId && o.Term == term && o.Year == year)
                .ToListAsync();

            if (obligations.Any())
            {
                return obligations.Sum(o => o.OutstandingAmount);
            }

            // Check for custom fee
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);

            if (customFee != null)
            {
                var paidAmountCustom = await GetPaidAmountForCustomFee(customFee.Id, studentId, term, year);
                return Math.Max(customFee.Amount - paidAmountCustom, 0);
            }

            // CRITICAL FIX: Get historical fee structure for the correct grade and year
            var termFee = await _historicalFeeService.GetTermFeeForGradeAndYearAsync(historicalGradeId, term, year);

            if (termFee == 0) return 0;

            var paidAmountRegular = await GetPaidAmountForTuition(studentId, term, year);

            return Math.Max(termFee - paidAmountRegular, 0);
        }

        /// <summary>
        /// Calculates cumulative arrears from enrollment to specified term (exclusive).
        /// Uses historical grades and fee structures.
        /// </summary>
        public async Task<decimal> CalculateCumulativeArrears(Guid studentId, string upToTerm, int upToYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 0;

            var termsInOrder = new[] { "Term 1", "Term 2", "Term 3" };
            decimal cumulativeArrears = 0;

            for (int year = student.EnrollmentYear; year <= upToYear; year++)
            {
                foreach (var term in termsInOrder)
                {
                    // Skip terms before student enrollment
                    if (year == student.EnrollmentYear &&
                        CompareTerms(term, student.EnrollmentTerm) < 0)
                    {
                        continue;
                    }

                    // Stop when we reach the specified term (don't include current term)
                    if (year == upToYear && term == upToTerm)
                    {
                        break;
                    }

                    if (year > upToYear)
                    {
                        break;
                    }

                    // This now uses historical grades and fees automatically
                    var outstandingForTerm = await CalculateOutstandingFees(studentId, term, year);
                    cumulativeArrears += outstandingForTerm;
                }
            }

            return cumulativeArrears;
        }

        private async Task<decimal> GetPaidAmountForCustomFee(Guid customFeeId, Guid studentId, string term, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeId == customFeeId &&
                           fp.FeeType == "Custom Tuition" &&
                           fp.Payment.StudentId == studentId &&
                           fp.Term == term &&
                           fp.Year == year &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => (decimal?)fp.Amount) ?? 0;
        }

        private async Task<decimal> GetPaidAmountForTuition(Guid studentId, string term, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeType == "Tuition" &&
                           fp.Payment.StudentId == studentId &&
                           fp.Term == term &&
                           fp.Year == year &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => (decimal?)fp.Amount) ?? 0;
        }

        private int CompareTerms(string term1, string term2)
        {
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            return Array.IndexOf(terms, term1) - Array.IndexOf(terms, term2);
        }
    }
}
