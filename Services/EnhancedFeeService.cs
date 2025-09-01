using Financial_management_backend.Data;
using Financial_management_backend.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IEnhancedFeeService
    {
        Task<List<FeeAllocationDto>> CalculateOptimalFeeAllocation(Guid studentId, decimal paymentAmount, DateTime paymentDate);
        Task<decimal> GetOutstandingBalance(Guid studentId, string? upToTerm = null, int? upToYear = null);
    }

    public class EnhancedFeeService : IEnhancedFeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly FeeService _feeService;
        private readonly IAcademicTermService _academicTermService;

        public EnhancedFeeService(ApplicationDbContext context, FeeService feeService, IAcademicTermService academicTermService)
        {
            _context = context;
            _feeService = feeService;
            _academicTermService = academicTermService;
        }

        public async Task<List<FeeAllocationDto>> CalculateOptimalFeeAllocation(Guid studentId, decimal paymentAmount, DateTime paymentDate)
        {
            var allocations = new List<FeeAllocationDto>();
            var remainingAmount = paymentAmount;
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return allocations;

            var (currentTerm, currentYear) = _academicTermService.GetAcademicTermForDate(paymentDate);
            var terms = new[] { "Term 1", "Term 2", "Term 3" };

            // Start from student's enrollment and allocate chronologically
            for (int year = student.EnrollmentYear; year <= currentYear && remainingAmount > 0; year++)
            {
                foreach (var term in terms)
                {
                    if (remainingAmount <= 0) break;

                    // Skip terms before enrollment
                    if (year == student.EnrollmentYear && 
                        Array.IndexOf(terms, term) < Array.IndexOf(terms, student.EnrollmentTerm))
                        continue;

                    // Don't allocate to future terms
                    if (year == currentYear && 
                        Array.IndexOf(terms, term) > Array.IndexOf(terms, currentTerm))
                        break;

                    var outstanding = await _feeService.CalculateOutstandingFees(studentId, term, year);
                    if (outstanding > 0)
                    {
                        var allocationAmount = Math.Min(remainingAmount, outstanding);
                        
                        // Allocate to tuition first, then other fees
                        var tuitionAllocation = await AllocateToTuition(studentId, term, year, allocationAmount);
                        allocations.AddRange(tuitionAllocation.allocations);
                        remainingAmount -= tuitionAllocation.allocated;

                        if (remainingAmount > 0)
                        {
                            var otherFeeAllocations = await AllocateToOtherFees(studentId, term, year, remainingAmount);
                            allocations.AddRange(otherFeeAllocations.allocations);
                            remainingAmount -= otherFeeAllocations.allocated;
                        }
                    }
                }
            }

            return allocations;
        }

        private async Task<(List<FeeAllocationDto> allocations, decimal allocated)> AllocateToTuition(
            Guid studentId, string term, int year, decimal availableAmount)
        {
            var allocations = new List<FeeAllocationDto>();
            var student = await _context.Students.FindAsync(studentId);
            
            // Check for custom fee first
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);

            if (customFee != null)
            {
                var paid = await GetPaidAmountForCustomFee(customFee.Id, term, year);
                var outstanding = Math.Max(customFee.Amount - paid, 0);
                var allocation = Math.Min(availableAmount, outstanding);

                if (allocation > 0)
                {
                    allocations.Add(new FeeAllocationDto
                    {
                        FeeId = customFee.Id,
                        FeeType = "Custom Tuition",
                        FeeSource = "CustomFee",
                        Term = term,
                        Amount = allocation
                    });
                }
                return (allocations, allocation);
            }

            // Regular tuition fee
            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.GradeId == student.GradeId);
            
            if (feeStructure != null)
            {
                var termFee = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };

                var paid = await GetPaidAmountForTuition(studentId, term, year);
                var outstanding = Math.Max(termFee - paid, 0);
                var allocation = Math.Min(availableAmount, outstanding);

                if (allocation > 0)
                {
                    allocations.Add(new FeeAllocationDto
                    {
                        FeeId = feeStructure.Id,
                        FeeType = "Tuition",
                        FeeSource = "FeeStructure",
                        Term = term,
                        Amount = allocation
                    });
                }
                return (allocations, allocation);
            }

            return (allocations, 0);
        }

        private async Task<(List<FeeAllocationDto> allocations, decimal allocated)> AllocateToOtherFees(
            Guid studentId, string term, int year, decimal availableAmount)
        {
            var allocations = new List<FeeAllocationDto>();
            var totalAllocated = 0m;
            var student = await _context.Students.FindAsync(studentId);

            var otherFees = await _context.OtherFees
                .Where(of => of.GradeId == student.GradeId)
                .ToListAsync();

            foreach (var otherFee in otherFees)
            {
                if (availableAmount <= 0) break;

                var paid = await GetPaidAmountForOtherFee(otherFee.Id, studentId, term, year);
                var outstanding = Math.Max(otherFee.Amount - paid, 0);
                var allocation = Math.Min(availableAmount, outstanding);

                if (allocation > 0)
                {
                    allocations.Add(new FeeAllocationDto
                    {
                        FeeId = otherFee.Id,
                        FeeType = otherFee.Name,
                        FeeSource = "OtherFee",
                        Term = term,
                        Amount = allocation
                    });

                    availableAmount -= allocation;
                    totalAllocated += allocation;
                }
            }

            return (allocations, totalAllocated);
        }

        private async Task<decimal> GetPaidAmountForTuition(Guid studentId, string term, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.Payment.StudentId == studentId && 
                           fp.FeeType == "Tuition" && 
                           fp.Payment.Term == term && 
                           fp.Payment.PaymentDate.Year == year)
                .SumAsync(fp => fp.Amount);
        }

        private async Task<decimal> GetPaidAmountForCustomFee(Guid customFeeId, string term, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeId == customFeeId && 
                           fp.FeeType == "Custom Tuition")
                .SumAsync(fp => fp.Amount);
        }

        private async Task<decimal> GetPaidAmountForOtherFee(Guid otherFeeId, Guid studentId, string term, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeId == otherFeeId && 
                           fp.Payment.StudentId == studentId)
                .SumAsync(fp => fp.Amount);
        }

        public async Task<decimal> GetOutstandingBalance(Guid studentId, string? upToTerm = null, int? upToYear = null)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var endTerm = upToTerm ?? currentTerm;
            var endYear = upToYear ?? currentYear;

            return await _feeService.CalculateCumulativeArrears(studentId, endTerm, endYear);
        }
    }
}