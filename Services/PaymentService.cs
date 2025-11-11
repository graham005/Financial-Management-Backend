using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFinancialTransactionService _transactionService;
        private readonly IAcademicTermService _academicTermService;

        public PaymentService(
            ApplicationDbContext context,
            IFinancialTransactionService transactionService,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _transactionService = transactionService;
            _academicTermService = academicTermService;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(PaymentDto paymentDto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate student
                var student = await _context.Students
                    .Include(s => s.Grade)
                    .FirstOrDefaultAsync(s => s.Id == paymentDto.StudentId);

                if (student == null)
                    throw new InvalidOperationException("Student not found.");

                // Validate fee allocations
                if (paymentDto.FeeAllocations == null || !paymentDto.FeeAllocations.Any())
                    throw new ArgumentException("At least one fee allocation is required.");

                var totalAllocated = paymentDto.FeeAllocations.Sum(f => f.Amount);
                if (Math.Abs(totalAllocated - paymentDto.Amount) > 0.01m)
                    throw new ArgumentException("Total allocated amount must equal payment amount.");

                // Create the payment
                var payment = new Payment
                {
                    StudentId = paymentDto.StudentId,
                    Amount = paymentDto.Amount,
                    PaymentDate = paymentDto.PaymentDate,
                    PaymentMethod = paymentDto.PaymentMethod,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                // Create fee payment allocations
                var feePayments = new List<FeePayment>();
                foreach (var allocation in paymentDto.FeeAllocations)
                {
                    // Validate term
                    if (!_academicTermService.IsValidTerm(allocation.Term))
                        throw new ArgumentException($"Invalid term: {allocation.Term}");

                    var feePayment = new FeePayment
                    {
                        PaymentId = payment.Id,
                        FeeId = allocation.FeeId,
                        FeeType = allocation.FeeType,
                        Term = allocation.Term,
                        Year = allocation.Year,
                        Amount = allocation.Amount,
                    };

                    feePayments.Add(feePayment);
                }

                await _context.FeePayments.AddRangeAsync(feePayments);
                await _context.SaveChangesAsync();

                // Create financial transaction
                var financialTransaction = new FinancialTransaction
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Type = "FeePayment",
                    Date = payment.PaymentDate,
                    CreatedBy = userId,
                    Description = $"Payment from {student.Name} - {paymentDto.PaymentMethod}",
                    Status = "Completed"
                };

                var createdTransaction = await _transactionService.CreateAsync(financialTransaction);

                await transaction.CommitAsync();

                // Load payment with relationships for response
                payment.FeePayments = feePayments;
                payment.Student = student;

                // Return comprehensive response
                return new PaymentResponseDto
                {
                    PaymentId = payment.Id,
                    TransactionId = createdTransaction.Id,
                    StudentId = student.Id,
                    StudentName = student.Name,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    Status = payment.Status,
                    FeeAllocations = paymentDto.FeeAllocations,
                    CreatedAt = payment.CreatedAt
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Payment> GetPaymentByIdAsync(Guid id)
        {
            var payment = await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.FeePayments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                throw new InvalidOperationException($"Payment with id '{id}' was not found.");

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStudentAsync(Guid studentId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.FeePayments)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<AvailableFeesDto> GetAvailableFeesForStudentAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            var availableFees = new List<AvailableFeeItemDto>();

            // Get current term
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            // Get tuition fees from FeeStructure
            var feeStructures = await _context.FeeStructures
                .Where(fs => fs.GradeId == student.GradeId)
                .ToListAsync();

            foreach (var feeStructure in feeStructures)
            {
                var paidAmount = await _context.FeePayments
                    .Where(fp => fp.FeeId == feeStructure.Id &&
                                fp.FeeType == "Tuition" &&
                                fp.Payment.StudentId == studentId &&
                                fp.Term == currentTerm &&
                                fp.Year == currentYear)
                    .SumAsync(fp => fp.Amount);

                var outstanding = feeStructure.TotalFee - paidAmount;

                if (outstanding > 0)
                {
                    availableFees.Add(new AvailableFeeItemDto
                    {
                        FeeId = feeStructure.Id,
                        FeeType = "Tuition",
                        FeeSource = "FeeStructure",
                        Term = currentTerm,
                        Year = currentYear,
                        TotalAmount = feeStructure.TotalFee,
                        PaidAmount = paidAmount,
                        OutstandingAmount = outstanding,
                        Description = $"Tuition Fee - {student.Grade.Name}",
                        IsOverdue = false // Implement your overdue logic
                    });
                }
            }

            // Get other fees
            var otherFees = await _context.OtherFees.ToListAsync();
            foreach (var otherFee in otherFees)
            {
                var paidAmount = await _context.FeePayments
                    .Where(fp => fp.FeeId == otherFee.Id &&
                                fp.FeeType == otherFee.Name &&
                                fp.Payment.StudentId == studentId &&
                                fp.Term == currentTerm &&
                                fp.Year == currentYear)
                    .SumAsync(fp => fp.Amount);

                var outstanding = otherFee.Amount - paidAmount;

                if (outstanding > 0)
                {
                    availableFees.Add(new AvailableFeeItemDto
                    {
                        FeeId = otherFee.Id,
                        FeeType = otherFee.Name,
                        FeeSource = "OtherFee",
                        Term = currentTerm,
                        Year = currentYear,
                        TotalAmount = otherFee.Amount,
                        PaidAmount = paidAmount,
                        OutstandingAmount = outstanding,
                        IsOverdue = false
                    });
                }
            }

            return new AvailableFeesDto
            {
                StudentId = studentId,
                StudentName = student.Name,
                AvailableFees = availableFees,
                TotalOutstanding = availableFees.Sum(f => f.OutstandingAmount)
            };
        }
    }
}