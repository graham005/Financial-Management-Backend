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

            // First check if there's a custom fee for this student/term/year
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);

            decimal requiredFee = 0;

            if (customFee != null)
            {
                // Use custom fee amount
                requiredFee = customFee.Amount;
                
                // Calculate what has been paid towards this custom fee
                var paidAmountCustom = await _context.FeePayments
                    .Where(fp => fp.FeeId == customFee.Id && fp.FeeType == "Custom Tuition")
                    .SumAsync(fp => (decimal?)fp.Amount) ?? 0;

                return Math.Max(requiredFee - paidAmountCustom, 0);
            }
            else
            {
                // Use regular fee structure
                var feeStructure = await _context.FeeStructures
                    .FirstOrDefaultAsync(fs => fs.GradeId == student.GradeId);

                if (feeStructure == null) return 0;

                // Get the required fee for this term
                requiredFee = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };

                if (requiredFee == 0) return 0;

                // Calculate what has been paid towards regular tuition for this term/year
                var paidAmountRegular = await _context.FeePayments
                    .Where(fp => fp.FeeId == feeStructure.Id && 
                               fp.FeeType == "Tuition" &&
                               fp.Payment.StudentId == studentId &&
                               fp.Payment.Term == term &&
                               fp.Payment.PaymentDate.Year == year &&
                               fp.Payment.Status == "Completed")
                    .SumAsync(fp => (decimal?)fp.Amount) ?? 0;

                return Math.Max(requiredFee - paidAmountRegular, 0);
            }
        }

        public async Task<decimal> CalculateCumulativeArrears(Guid studentId, string upToTerm, int upToYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 0;

            var termsInOrder = new[] { "Term 1", "Term 2", "Term 3" };
            decimal cumulativeArrears = 0;

            // Calculate arrears from enrollment year up to (but not including) the specified term/year
            for (int year = student.EnrollmentYear; year <= upToYear; year++)
            {
                foreach (var term in termsInOrder)
                {
                    // Skip terms before student enrollment
                    if (year == student.EnrollmentYear && 
                        Array.IndexOf(termsInOrder, term) < Array.IndexOf(termsInOrder, student.EnrollmentTerm))
                    {
                        continue;
                    }

                    // Stop when we reach the specified term in the specified year (don't include current term)
                    if (year == upToYear && term == upToTerm)
                    {
                        break;
                    }

                    // If we're past the specified year, stop
                    if (year > upToYear)
                    {
                        break;
                    }

                    // Calculate outstanding fees for this term using the same logic as CalculateOutstandingFees
                    var outstandingForTerm = await CalculateOutstandingFees(studentId, term, year);
                    cumulativeArrears += outstandingForTerm;
                }
            }

            return cumulativeArrears;
        }
    }
}
