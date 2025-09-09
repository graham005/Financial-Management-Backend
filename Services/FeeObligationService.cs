using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public class FeeObligationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAcademicTermService _academicTermService;

        public FeeObligationService(
            ApplicationDbContext context,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _academicTermService = academicTermService;
        }

        public async Task GenerateObligationsForTerm(string term, int year)
        {
            var students = await _context.Students
                .Include(s => s.Grade)
                .ToListAsync();
                
            foreach (var student in students)
            {
                // Skip if student enrolled after this term
                if (year < student.EnrollmentYear || 
                    (year == student.EnrollmentYear && 
                     CompareTerms(term, student.EnrollmentTerm) < 0))
                {
                    continue;
                }
                
                await GenerateObligationsForStudent(student.Id, term, year);
            }
        }

        public async Task GenerateObligationsForStudent(Guid studentId, string term, int year)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);
                
            if (student == null) return;
            
            // Check if obligations already exist
            var existingObligations = await _context.StudentFeeObligations
                .AnyAsync(fo => fo.StudentId == studentId && fo.Term == term && fo.Year == year);
                
            if (existingObligations) return; // Don't duplicate
            
            // First check for custom fee
            var customFee = await _context.CustomFees
                .FirstOrDefaultAsync(cf => cf.StudentId == studentId && cf.Term == term && cf.Year == year);
                
            if (customFee != null)
            {
                // Create obligation for custom fee
                await _context.StudentFeeObligations.AddAsync(new StudentFeeObligation
                {
                    StudentId = studentId,
                    Term = term,
                    Year = year,
                    FeeType = "Custom Tuition",
                    FeeSourceId = customFee.Id,
                    FeeSourceType = "CustomFee",
                    AmountCharged = customFee.Amount,
                    AmountPaid = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Find the historical fee structure that was in effect for this term/year
                var feeStructureHistory = await _context.FeeStructureHistories
                    .Where(fsh => fsh.GradeId == student.GradeId && 
                                  fsh.AcademicYear <= year)
                    .OrderByDescending(fsh => fsh.AcademicYear)
                    .ThenByDescending(fsh => fsh.EffectiveFrom)
                    .FirstOrDefaultAsync();
                    
                if (feeStructureHistory != null)
                {
                    // Determine the fee amount based on the term
                    decimal termFee = term switch
                    {
                        "Term 1" => feeStructureHistory.Term1Fee,
                        "Term 2" => feeStructureHistory.Term2Fee,
                        "Term 3" => feeStructureHistory.Term3Fee,
                        _ => 0
                    };
                    
                    if (termFee > 0)
                    {
                        // Create obligation for tuition based on historical rates
                        await _context.StudentFeeObligations.AddAsync(new StudentFeeObligation
                        {
                            StudentId = studentId,
                            Term = term,
                            Year = year,
                            FeeType = "Tuition",
                            FeeSourceId = feeStructureHistory.Id,
                            FeeSourceType = "FeeStructureHistory",
                            AmountCharged = termFee,
                            AmountPaid = 0,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            
            // Add other fees (like books, activity fees, etc.)
            var otherFees = await _context.OtherFees
                .Where(of => of.GradeId == student.GradeId)
                .ToListAsync();
                
            foreach (var otherFee in otherFees)
            {
                await _context.StudentFeeObligations.AddAsync(new StudentFeeObligation
                {
                    StudentId = studentId,
                    Term = term,
                    Year = year,
                    FeeType = otherFee.Name,
                    FeeSourceId = otherFee.Id,
                    FeeSourceType = "OtherFee",
                    AmountCharged = otherFee.Amount,
                    AmountPaid = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            await _context.SaveChangesAsync();
        }
        
        // Helper to compare terms chronologically
        private int CompareTerms(string term1, string term2)
        {
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            return Array.IndexOf(terms, term1) - Array.IndexOf(terms, term2);
        }
        
        // Update obligations whenever a payment is made
        public async Task UpdateObligationPayments(Guid studentId, IEnumerable<FeePayment> feePayments)
        {
            foreach (var payment in feePayments)
            {
                // Find the matching obligation
                var obligation = await _context.StudentFeeObligations
                    .FirstOrDefaultAsync(o => 
                        o.StudentId == studentId && 
                        o.Term == payment.Term && 
                        o.Year == payment.Year && 
                        o.FeeType == payment.FeeType);
                        
                if (obligation != null)
                {
                    // Update the amount paid
                    obligation.AmountPaid += payment.Amount;
                }
            }
            
            await _context.SaveChangesAsync();
        }
    }
}