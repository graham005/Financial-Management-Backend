using Financial_management_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public class FeeService
    {
        private readonly ApplicationDbContext _context;

        public FeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> CalculateOutstandingFees(Guid studentId, string term, int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 0;

            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);

            decimal termFee = customFee?.Amount ?? 0;

            if (customFee == null)
            {
                var feeStructure = await _context.FeeStructures.FirstOrDefaultAsync(fs => fs.GradeId == student.GradeId);
                if (feeStructure == null) return 0;

                termFee = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };
            }

            var totalPaymentsForTerm = await _context.Payments
                .Where(p => p.StudentId == studentId && p.Term == term && p.PaymentDate.Year == year)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Math.Max(termFee - totalPaymentsForTerm, 0);
        }

        public async Task<decimal> CalculateCumulativeArrears(Guid studentId, string currentTerm, int currentYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 0;

            var termsInOrder = new[] { "Term 1", "Term 2", "Term 3" };

            decimal cumulativeArrears = 0;

            foreach (var term in termsInOrder)
            {
                if (currentYear < student.EnrollmentYear ||
                    (currentYear == student.EnrollmentYear && Array.IndexOf(termsInOrder, term) < Array.IndexOf(termsInOrder, student.EnrollmentTerm)))
                {
                    continue;
                }

                if (term == currentTerm) break;

                var termFee = await CalculateOutstandingFees(studentId, term, currentYear);
                cumulativeArrears += termFee;
            }

            return cumulativeArrears;
        }
    }
}
