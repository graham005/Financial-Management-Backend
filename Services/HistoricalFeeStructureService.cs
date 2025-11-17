using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IHistoricalFeeStructureService
    {
        Task<(decimal Term1Fee, decimal Term2Fee, decimal Term3Fee)?> GetFeeStructureForGradeAndYearAsync(Guid gradeId, int year);
        Task<decimal> GetTermFeeForGradeAndYearAsync(Guid gradeId, string term, int year);
    }

    public class HistoricalFeeStructureService : IHistoricalFeeStructureService
    {
        private readonly ApplicationDbContext _context;

        public HistoricalFeeStructureService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the appropriate fee structure for a grade in a specific year.
        /// Priority: Historical fee for exact year > Closest historical fee before year > Current fee structure
        /// </summary>
        public async Task<(decimal Term1Fee, decimal Term2Fee, decimal Term3Fee)?> GetFeeStructureForGradeAndYearAsync(Guid gradeId, int year)
        {
            // Priority 1: Try to find historical fee structure for this exact year
            var exactYearFee = await _context.FeeStructureHistories
                .Where(fsh => fsh.GradeId == gradeId && fsh.AcademicYear == year)
                .OrderByDescending(fsh => fsh.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (exactYearFee != null)
            {
                return (exactYearFee.Term1Fee, exactYearFee.Term2Fee, exactYearFee.Term3Fee);
            }

            // Priority 2: Find the closest historical record before or at this year
            var closestHistoricalFee = await _context.FeeStructureHistories
                .Where(fsh => fsh.GradeId == gradeId && fsh.AcademicYear <= year)
                .OrderByDescending(fsh => fsh.AcademicYear)
                .ThenByDescending(fsh => fsh.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (closestHistoricalFee != null)
            {
                return (closestHistoricalFee.Term1Fee, closestHistoricalFee.Term2Fee, closestHistoricalFee.Term3Fee);
            }

            // Priority 3: Fall back to current fee structure
            var currentFeeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.GradeId == gradeId);

            if (currentFeeStructure != null)
            {
                return (currentFeeStructure.Term1Fee, currentFeeStructure.Term2Fee, currentFeeStructure.Term3Fee);
            }

            return null;
        }

        /// <summary>
        /// Gets the fee for a specific term, grade, and year.
        /// </summary>
        public async Task<decimal> GetTermFeeForGradeAndYearAsync(Guid gradeId, string term, int year)
        {
            var feeStructure = await GetFeeStructureForGradeAndYearAsync(gradeId, year);

            if (!feeStructure.HasValue)
                return 0;

            return term switch
            {
                "Term 1" => feeStructure.Value.Term1Fee,
                "Term 2" => feeStructure.Value.Term2Fee,
                "Term 3" => feeStructure.Value.Term3Fee,
                _ => 0
            };
        }
    }
}