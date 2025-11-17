using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IStudentGradeHistoryService
    {
        Task<Guid> GetStudentGradeForTermAsync(Guid studentId, string term, int year);
        Task<List<(string Term, int Year, Guid GradeId, string GradeName, int Level)>> GetCompleteGradeHistoryAsync(Guid studentId);
        Task BackfillStudentGradeHistoryAsync(Guid studentId);
        Task BackfillAllStudentsGradeHistoryAsync();
    }

    public class StudentGradeHistoryService : IStudentGradeHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAcademicTermService _academicTermService;

        public StudentGradeHistoryService(
            ApplicationDbContext context,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _academicTermService = academicTermService;
        }

        /// <summary>
        /// Gets the grade a student was in during a specific term/year.
        /// Calculates based on enrollment and current grade using levels.
        /// </summary>
        public async Task<Guid> GetStudentGradeForTermAsync(Guid studentId, string term, int year)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            // If requesting current term/year, return current grade
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            if (year == currentYear && term == currentTerm)
                return student.GradeId;

            // If requesting future term, return current grade (shouldn't happen normally)
            if (year > currentYear || (year == currentYear && CompareTerms(term, currentTerm) > 0))
                return student.GradeId;

            // If requesting before enrollment, throw error
            if (year < student.EnrollmentYear || 
                (year == student.EnrollmentYear && CompareTerms(term, student.EnrollmentTerm) < 0))
            {
                throw new InvalidOperationException($"Student was not enrolled in {term} {year}");
            }

            // Calculate how many academic years have passed since enrollment
            int yearsSinceEnrollment = currentYear - year;
            
            // If in the same year but different term, check if we need to subtract a year
            if (year == currentYear)
            {
                yearsSinceEnrollment = 0;
            }
            else if (year == student.EnrollmentYear)
            {
                yearsSinceEnrollment = currentYear - student.EnrollmentYear;
            }

            // Calculate the grade level the student was in during that term
            int historicalLevel = student.Grade.Level - yearsSinceEnrollment;

            // Find the grade with that level
            var historicalGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.Level == historicalLevel);

            if (historicalGrade == null)
            {
                // Fallback to current grade if calculation fails
                return student.GradeId;
            }

            return historicalGrade.Id;
        }

        /// <summary>
        /// Gets complete grade history for a student from enrollment to current.
        /// </summary>
        public async Task<List<(string Term, int Year, Guid GradeId, string GradeName, int Level)>> GetCompleteGradeHistoryAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            var history = new List<(string Term, int Year, Guid GradeId, string GradeName, int Level)>();
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            var terms = new[] { "Term 1", "Term 2", "Term 3" };

            // Start from enrollment year
            for (int year = student.EnrollmentYear; year <= currentYear; year++)
            {
                foreach (var term in terms)
                {
                    // Skip terms before enrollment
                    if (year == student.EnrollmentYear && 
                        CompareTerms(term, student.EnrollmentTerm) < 0)
                        continue;

                    // Skip future terms
                    if (year == currentYear && 
                        CompareTerms(term, currentTerm) > 0)
                        continue;

                    try
                    {
                        var gradeId = await GetStudentGradeForTermAsync(studentId, term, year);
                        var grade = await _context.Grades.FindAsync(gradeId);

                        if (grade != null)
                        {
                            history.Add((term, year, gradeId, grade.Name, grade.Level));
                        }
                    }
                    catch
                    {
                        // Skip if error calculating grade for this term
                        continue;
                    }
                }
            }

            return history;
        }

        /// <summary>
        /// Backfills grade history for a single student (logs or stores for audit).
        /// </summary>
        public async Task BackfillStudentGradeHistoryAsync(Guid studentId)
        {
            var history = await GetCompleteGradeHistoryAsync(studentId);
            
            // You can log this or store it in a separate table if needed
            // For now, we're just calculating it on-demand
            Console.WriteLine($"Backfilled history for student {studentId}:");
            foreach (var (term, year, gradeId, gradeName, level) in history)
            {
                Console.WriteLine($"  {term} {year}: {gradeName} (Level {level})");
            }
        }

        /// <summary>
        /// Backfills grade history for all students.
        /// </summary>
        public async Task BackfillAllStudentsGradeHistoryAsync()
        {
            var students = await _context.Students.ToListAsync();

            foreach (var student in students)
            {
                try
                {
                    await BackfillStudentGradeHistoryAsync(student.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error backfilling history for student {student.Id}: {ex.Message}");
                }
            }
        }

        private int CompareTerms(string term1, string term2)
        {
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            return Array.IndexOf(terms, term1) - Array.IndexOf(terms, term2);
        }
    }
}