using Financial_management_backend.Data;
using Financial_management_backend.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IFeeValidationService
    {
        Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidateFeeAllocationAsync(FeeAllocationDto allocation, Guid studentId);
        Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidatePaymentAsync(PaymentDto payment);
    }

    public class FeeValidationService : IFeeValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly FeeService _feeService;

        public FeeValidationService(ApplicationDbContext context, FeeService feeService)
        {
            _context = context;
            _feeService = feeService;
        }

        public async Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidatePaymentAsync(PaymentDto payment)
        {
            // Validate student exists
            var student = await _context.Students.FindAsync(payment.StudentId);
            if (student == null)
                return (false, "Student not found", "");

            // Validate payment amount is positive
            if (payment.Amount <= 0)
                return (false, "Payment amount must be positive", "");

            // Validate fee allocations exist
            if (payment.FeeAllocations == null || !payment.FeeAllocations.Any())
                return (false, "Fee allocations are required", "");

            string combinedWarnings = "";

            // Validate each fee allocation
            foreach (var allocation in payment.FeeAllocations)
            {
                var (isValid, errorMessage, warningMessage) = await ValidateFeeAllocationAsync(allocation, payment.StudentId);

                if (!isValid)
                    return (false, errorMessage, "");

                if (!string.IsNullOrEmpty(warningMessage))
                    combinedWarnings += warningMessage + " ";
            }

            // Validate total amounts match
            var totalAllocated = payment.FeeAllocations.Sum(fa => fa.Amount);
            if (Math.Abs(totalAllocated - payment.Amount) > 0.01m)
                return (false, $"Total allocated amount ({totalAllocated:C}) does not match payment amount ({payment.Amount:C})", "");

            return (true, "", combinedWarnings.Trim());
        }

        public async Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidateFeeAllocationAsync(FeeAllocationDto allocation, Guid studentId)
        {
            // Validate allocation amount is positive
            if (allocation.Amount <= 0)
                return (false, "Fee allocation amount must be positive", "");

            // Validate required fields
            if (string.IsNullOrEmpty(allocation.Term))
                return (false, "Term is required for fee allocation", "");

            if (allocation.Year <= 0)
                return (false, "Year is required for fee allocation", "");

            if (string.IsNullOrEmpty(allocation.FeeType))
                return (false, "Fee type is required for fee allocation", "");

            if (string.IsNullOrEmpty(allocation.FeeSource))
                return (false, "Fee source is required for fee allocation", "");

            return allocation.FeeSource?.ToLower() switch
            {
                "feestructure" => await ValidateFeeStructureAsync(allocation, studentId),
                "otherfee" => await ValidateOtherFeeAsync(allocation, studentId),
                "customfee" => await ValidateCustomFeeAsync(allocation, studentId),
                _ => (false, $"Invalid fee source: {allocation.FeeSource}", "")
            };
        }

        private async Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidateFeeStructureAsync(
            FeeAllocationDto allocation, Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.Id == allocation.FeeId && fs.GradeId == student.GradeId);

            if (feeStructure == null)
                return (false, "Fee structure not found for student's grade", "");

            decimal expectedAmount = allocation.Term switch
            {
                "Term 1" => feeStructure.Term1Fee,
                "Term 2" => feeStructure.Term2Fee,
                "Term 3" => feeStructure.Term3Fee,
                _ => 0
            };

            if (expectedAmount == 0)
                return (false, $"Invalid term: {allocation.Term}", "");

            // Use FeePayment Term/Year for validation
            var paidAmount = await _context.FeePayments
                .Where(fp => fp.FeeId == allocation.FeeId && 
                           fp.Payment.StudentId == studentId &&
                           fp.FeeType == "Tuition" &&
                           fp.Term == allocation.Term &&
                           fp.Year == allocation.Year &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => (decimal?)fp.Amount) ?? 0;

            var outstandingAmount = Math.Max(expectedAmount - paidAmount, 0);
            string warningMessage = "";

            if (allocation.Amount > outstandingAmount && outstandingAmount > 0)
            {
                var excess = allocation.Amount - outstandingAmount;
                warningMessage = $"Excess payment of {excess:C} for {allocation.Term} {allocation.Year} tuition. This will be treated as advance payment.";
            }
            else if (outstandingAmount <= 0)
            {
                warningMessage = $"Tuition for {allocation.Term} {allocation.Year} is already fully paid. This is an overpayment.";
            }

            return (true, "", warningMessage);
        }

        private async Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidateOtherFeeAsync(
            FeeAllocationDto allocation, Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var otherFee = await _context.OtherFees
                .FirstOrDefaultAsync(of => of.Id == allocation.FeeId && of.GradeId == student.GradeId);

            if (otherFee == null)
                return (false, "Other fee not found for student's grade", "");

            // UPDATED: Use FeePayment Term/Year for validation
            var paidForThisFee = await _context.FeePayments
                .Where(fp => fp.FeeId == allocation.FeeId && 
                           fp.Payment.StudentId == studentId &&
                           fp.Term == allocation.Term &&
                           fp.Year == allocation.Year &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => (decimal?)fp.Amount) ?? 0;

            var remainingAmount = Math.Max(otherFee.Amount - paidForThisFee, 0);
            
            string warningMessage = "";
            if (allocation.Amount > remainingAmount && remainingAmount > 0)
            {
                var excess = allocation.Amount - remainingAmount;
                warningMessage = $"Excess payment of {excess:C} for {otherFee.Name} ({allocation.Term} {allocation.Year}). Student has overpaid.";
            }
            else if (remainingAmount <= 0)
            {
                warningMessage = $"{otherFee.Name} for {allocation.Term} {allocation.Year} is already fully paid. This is an overpayment.";
            }

            return (true, "", warningMessage);
        }

        private async Task<(bool IsValid, string ErrorMessage, string WarningMessage)> ValidateCustomFeeAsync(
            FeeAllocationDto allocation, Guid studentId)
        {
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.Id == allocation.FeeId && 
                                         cf.StudentId == studentId && 
                                         cf.Term == allocation.Term &&
                                         cf.Year == allocation.Year);

            if (customFee == null)
                return (false, "Custom fee not found for student, term, and year", "");

            // UPDATED: Use FeePayment Term/Year for validation
            var paidForCustomFee = await _context.FeePayments
                .Where(fp => fp.FeeId == allocation.FeeId && 
                           fp.Payment.StudentId == studentId &&
                           fp.Term == allocation.Term &&
                           fp.Year == allocation.Year &&
                           fp.Payment.Status == "Completed")
                .SumAsync(fp => (decimal?)fp.Amount) ?? 0;

            var remainingAmount = Math.Max(customFee.Amount - paidForCustomFee, 0);
            
            string warningMessage = "";
            if (allocation.Amount > remainingAmount && remainingAmount > 0)
            {
                var excess = allocation.Amount - remainingAmount;
                warningMessage = $"Excess payment of {excess:C} for custom fee ({allocation.Term} {allocation.Year}). Student has overpaid.";
            }
            else if (remainingAmount <= 0)
            {
                warningMessage = $"Custom fee for {allocation.Term} {allocation.Year} is already fully paid. This is an overpayment.";
            }

            return (true, "", warningMessage);
        }
    }
}
