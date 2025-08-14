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

        public PaymentController(ApplicationDbContext context, FeeService feeService)
        {
            _context = context;
            _feeService = feeService;
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] PaymentDto paymentDto)
        {
            var student = await _context.Students.FindAsync(paymentDto.StudentId);
            if (student == null) { return NotFound("Student not found."); }

            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                StudentId = paymentDto.StudentId,
                Amount = paymentDto.Amount,
                PaymentDate = paymentDto.PaymentDate,
                Term = paymentDto.Term,
                PaymentMethod = paymentDto.PaymentMethod,
                Status = "Completed",
                CreatedBy = (Guid)userId
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

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
            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
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
                query = query.Where(p =>  p.StudentId == studentId);

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

        // GET: api/accountant/payment/student/{studentId}/arrears
        [HttpGet("student/{studentId}/arrears")]
        public async Task<IActionResult> GetStudentArrears(Guid studentId, int currentYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound("Student not found.");

            var cumulativeArrears = await _feeService.CalculateCumulativeArrears(studentId, "Term 3", currentYear);

            return Ok(new
            {
                StudentName = student.Name,
                student.EnrollmentTerm,
                student.EnrollmentYear,
                CumulativeArrears = cumulativeArrears // Negative if overpaid
            });
        }

        // GET: api/accountant/payment/grade/{gradeId}/arrears
        [HttpGet("grade/{gradeId}/arrears")]
        public async Task<IActionResult> GetGradeArrears(Guid gradeId, int currentYear)
        {
            var students = await _context.Students.Where(s => s.GradeId == gradeId).ToListAsync();

            if (!students.Any()) { return NotFound("No students found in this grade."); }

            var gradeArrears = students.Select(async student =>
            {
                var cumulativeArrears = await _feeService.CalculateCumulativeArrears(student.Id, "Term 3", currentYear);

                return new ArrearsResponseDto
                {
                    StudentName = student.Name,
                    EnrollementTerm = student.EnrollmentTerm,
                    EnrollmentYear = student.EnrollmentYear,
                    CumulativeArrears = cumulativeArrears,
                };
            });

            return Ok(await Task.WhenAll(gradeArrears));
        }

        // DELETE: api/accountant/payment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound("Payment not found.");

            // Instead of deleting, mark the payment as "Failed"
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
