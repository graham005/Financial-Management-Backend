using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Financial_management_backend.Services.Dtos;
using Financial_management_backend.Services.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Accountant
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FeeService _feeService;
        private readonly IFinancialTransactionService _transactionService;
        private readonly IFeeValidationService _feeValidationService;
        private readonly IAcademicTermService _academicTermService;

        public PaymentController(
            ApplicationDbContext context,
            FeeService feeService,
            IFinancialTransactionService transactionService,
            IFeeValidationService feeValidationService,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _feeService = feeService;
            _transactionService = transactionService;
            _feeValidationService = feeValidationService;
            _academicTermService = academicTermService;
        }

        // Get available fees for a student to help with payment allocation
        [HttpGet("student/{studentId}/available-fees")]
        public async Task<IActionResult> GetAvailableFeesForStudent(Guid studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Grade)
                    .FirstOrDefaultAsync(s => s.Id == studentId);
                
                if (student == null)
                    return NotFound("Student not found.");

                var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
                var availableFees = new List<AvailableFeeItemDto>();

                // Get all terms from enrollment to current
                var terms = new[] { "Term 1", "Term 2", "Term 3" };
                
                for (int year = student.EnrollmentYear; year <= currentYear; year++)
                {
                    foreach (var term in terms)
                    {
                        // Skip terms before enrollment
                        if (year == student.EnrollmentYear && 
                            Array.IndexOf(terms, term) < Array.IndexOf(terms, student.EnrollmentTerm))
                            continue;

                        // Add tuition fees
                        await AddTuitionFees(availableFees, studentId, term, year, student.GradeId);
                        
                        // Add other fees for current and future terms only
                        if (year >= currentYear)
                        {
                            await AddOtherFees(availableFees, studentId, term, year, student.GradeId);
                        }
                    }
                }

                var result = new AvailableFeesDto
                {
                    StudentId = studentId,
                    StudentName = student.Name,
                    AvailableFees = availableFees.Where(f => f.OutstandingAmount > 0).ToList(),
                    TotalOutstanding = availableFees.Sum(f => f.OutstandingAmount)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving available fees: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] PaymentDto paymentDto)
        {
            try
            {
                // Validate that fee allocations are provided
                if (paymentDto.FeeAllocations == null || !paymentDto.FeeAllocations.Any())
                    return BadRequest("Fee allocations must be specified. Use GET /student/{id}/available-fees to see available fees.");

                // Enhanced validation
                var (isValid, errorMessage, warningMessage) = await _feeValidationService.ValidatePaymentAsync(paymentDto);
                if (!isValid)
                    return BadRequest(errorMessage);

                var student = await _context.Students.FindAsync(paymentDto.StudentId);
                if (student == null) 
                    return NotFound("Student not found.");

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Auto-determine term from payment date
                var (paymentTerm, paymentYear) = _academicTermService.GetAcademicTermForDate(paymentDto.PaymentDate);

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    StudentId = paymentDto.StudentId,
                    Amount = paymentDto.Amount,
                    PaymentDate = paymentDto.PaymentDate,
                    Term = paymentTerm, // Auto-determined term
                    PaymentMethod = paymentDto.PaymentMethod,
                    Status = "Completed",
                    CreatedBy = (Guid)userId
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                // Create fee payment records based on user's allocation
                foreach (var feeAllocation in paymentDto.FeeAllocations)
                {
                    var feePayment = new FeePayment
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        FeeId = feeAllocation.FeeId,
                        FeeType = feeAllocation.FeeType,
                        Amount = feeAllocation.Amount,
                    };

                    await _context.FeePayments.AddAsync(feePayment);
                }

                await _context.SaveChangesAsync();

                // Create financial transaction
                var feeDetails = string.Join(", ", paymentDto.FeeAllocations.Select(fa => 
                    $"{fa.FeeType} ({fa.Term} {fa.Year}): {fa.Amount:C}"));

                var transaction = new FinancialTransaction
                {
                    Date = payment.PaymentDate,
                    Amount = payment.Amount,
                    Type = "FeePayment",
                    Category = "Fee",
                    Description = $"Payment for {student.Name} - {feeDetails}",
                    CreatedBy = (Guid)userId,
                    PaymentId = payment.Id,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };
                await _transactionService.CreateAsync(transaction);

                var response = new
                {
                    PaymentId = payment.Id,
                    Message = "Payment recorded successfully",
                    Warning = !string.IsNullOrEmpty(warningMessage) ? warningMessage : null,
                    FeeAllocations = paymentDto.FeeAllocations.Select(fa => new
                    {
                        fa.FeeType,
                        fa.Term,
                        fa.Year,
                        fa.Amount,
                        fa.Description
                    })
                };

                return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task AddTuitionFees(List<AvailableFeeItemDto> availableFees, Guid studentId, string term, int year, Guid gradeId)
        {
            // Check for custom fee first
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);

            if (customFee != null)
            {
                var paidAmount = await GetPaidAmountForFee(customFee.Id, "Custom Tuition");
                availableFees.Add(new AvailableFeeItemDto
                {
                    FeeId = customFee.Id,
                    FeeType = "Custom Tuition",
                    FeeSource = "CustomFee",
                    Term = term,
                    Year = year,
                    TotalAmount = customFee.Amount,
                    PaidAmount = paidAmount,
                    OutstandingAmount = Math.Max(customFee.Amount - paidAmount, 0),
                    Description = $"Custom tuition fee for {term} {year}",
                    IsOverdue = year < DateTime.Now.Year || (year == DateTime.Now.Year && IsTermOverdue(term))
                });
                return;
            }

            // Regular tuition fee
            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.GradeId == gradeId);

            if (feeStructure != null)
            {
                var termFee = term switch
                {
                    "Term 1" => feeStructure.Term1Fee,
                    "Term 2" => feeStructure.Term2Fee,
                    "Term 3" => feeStructure.Term3Fee,
                    _ => 0
                };

                if (termFee > 0)
                {
                    var paidAmount = await GetPaidAmountForTuition(studentId, term, year);
                    availableFees.Add(new AvailableFeeItemDto
                    {
                        FeeId = feeStructure.Id,
                        FeeType = "Tuition",
                        FeeSource = "FeeStructure",
                        Term = term,
                        Year = year,
                        TotalAmount = termFee,
                        PaidAmount = paidAmount,
                        OutstandingAmount = Math.Max(termFee - paidAmount, 0),
                        Description = $"Tuition fee for {term} {year}",
                        IsOverdue = year < DateTime.Now.Year || (year == DateTime.Now.Year && IsTermOverdue(term))
                    });
                }
            }
        }

        private async Task AddOtherFees(List<AvailableFeeItemDto> availableFees, Guid studentId, string term, int year, Guid gradeId)
        {
            var otherFees = await _context.OtherFees
                .Where(of => of.GradeId == gradeId)
                .ToListAsync();

            foreach (var otherFee in otherFees)
            {
                var paidAmount = await GetPaidAmountForOtherFee(otherFee.Id, studentId, year);
                availableFees.Add(new AvailableFeeItemDto
                {
                    FeeId = otherFee.Id,
                    FeeType = otherFee.Name,
                    FeeSource = "OtherFee",
                    Term = term,
                    Year = year,
                    TotalAmount = otherFee.Amount,
                    PaidAmount = paidAmount,
                    OutstandingAmount = Math.Max(otherFee.Amount - paidAmount, 0),
                    Description = $"{otherFee.Name} for {term} {year}",
                    IsOverdue = false // Other fees are typically not overdue
                });
            }
        }

        private async Task<decimal> GetPaidAmountForFee(Guid feeId, string feeType)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeId == feeId && fp.FeeType == feeType)
                .SumAsync(fp => fp.Amount);
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

        private async Task<decimal> GetPaidAmountForOtherFee(Guid otherFeeId, Guid studentId, int year)
        {
            return await _context.FeePayments
                .Where(fp => fp.FeeId == otherFeeId && 
                           fp.Payment.StudentId == studentId &&
                           fp.Payment.PaymentDate.Year == year)
                .SumAsync(fp => fp.Amount);
        }

        private bool IsTermOverdue(string term)
        {
            var (currentTerm, _) = _academicTermService.GetCurrentAcademicTerm();
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            return Array.IndexOf(terms, term) < Array.IndexOf(terms, currentTerm);
        }

        // GET: api/accountant/payment/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound("Payment not found.");

            return Ok(new
            {
                payment.Id,
                StudentName = payment.Student.Name,
                payment.Amount,
                payment.PaymentDate,
                payment.Term,
                payment.PaymentMethod,
                payment.Status,
            });
        }

        //GET: api/accountant/payment
        [HttpGet]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] Guid? studentId,
            [FromQuery] string term,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Payments
                .Include(p => p.Student)
                .AsQueryable();

            if (studentId.HasValue)
                query = query.Where(p => p.StudentId == studentId);

            if (!string.IsNullOrEmpty(term))
                query = query.Where(p => p.Term == term);

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);

            var payments = await query.ToListAsync();

            return Ok(payments.Select(p => new
            {
                p.Id,
                StudentName = p.Student.Name,
                p.Amount,
                p.PaymentDate,
                p.Term,
                p.PaymentMethod,
                p.Status
            }));
        }

        // PATCH: api/accountant/payment/{id}/fail
        [HttpPatch("{id}/fail")]
        public async Task<IActionResult> FailPayment(Guid id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) { return NotFound("Payment not found."); }

            payment.Status = "Failed";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                payment.Id,
                payment.Status
            });
        }

        // FIXED: GET: api/payment/student/{studentId}/arrears - now uses AcademicTermService
        [HttpGet("student/{studentId}/arrears")]
        public async Task<IActionResult> GetStudentArrears(Guid studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound("Student not found.");

            // Use AcademicTermService to get current term/year
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            // Calculate arrears up to the current term (but not including it)
            var cumulativeArrears = await _feeService.CalculateCumulativeArrears(studentId, currentTerm, currentYear);

            return Ok(new
            {
                StudentName = student.Name,
                student.EnrollmentTerm,
                student.EnrollmentYear,
                CurrentTerm = currentTerm,
                CurrentYear = currentYear,
                CumulativeArrears = cumulativeArrears,
                ArrearsStatus = cumulativeArrears > 0 ? "Has Arrears" : cumulativeArrears < 0 ? "Overpaid" : "Up to Date"
            });
        }

        // FIXED: GET: api/payment/grade/{gradeId}/arrears - now uses AcademicTermService
        [HttpGet("grade/{gradeId}/arrears")]
        public async Task<IActionResult> GetGradeArrears(Guid gradeId)
        {
            var students = await _context.Students.Where(s => s.GradeId == gradeId).ToListAsync();

            if (!students.Any()) { return NotFound("No students found in this grade."); }

            // Use AcademicTermService to get current term/year
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            var gradeArrears = await Task.WhenAll(students.Select(async student =>
            {
                var cumulativeArrears = await _feeService.CalculateCumulativeArrears(student.Id, currentTerm, currentYear);

                return new ArrearsResponseDto
                {
                    StudentName = student.Name,
                    EnrollementTerm = student.EnrollmentTerm,
                    EnrollmentYear = student.EnrollmentYear,
                    CumulativeArrears = cumulativeArrears,
                };
            }));

            return Ok(new
            {
                GradeId = gradeId,
                CurrentTerm = currentTerm,
                CurrentYear = currentYear,
                TotalStudents = students.Count,
                StudentsWithArrears = gradeArrears.Count(s => s.CumulativeArrears > 0),
                TotalArrears = gradeArrears.Sum(s => Math.Max(s.CumulativeArrears, 0)),
                Students = gradeArrears
            });
        }

        // DELETE: api/accountant/payment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound("Payment not found.");

            payment.Status = "Failed";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Payment marked as Failed",
                payment.Id,
                payment.Status
            });
        }
    }
}
