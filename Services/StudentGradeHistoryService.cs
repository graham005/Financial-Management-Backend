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

            // Get enrollment grade to calculate from
            var enrollmentGrade = await GetEnrollmentGradeAsync(student);

            // Calculate number of years of progression from enrollment to requested term
            int yearsProgressed = CalculateYearsProgressed(
                student.EnrollmentTerm, 
                student.EnrollmentYear, 
                term, 
                year
            );

            // Calculate the historical grade level
            int historicalLevel = enrollmentGrade.Level + yearsProgressed;

            // Find the grade with that level - ORDER BY Level to ensure consistent retrieval
            var historicalGrade = await _context.Grades
                .OrderBy(g => g.Level)
                .FirstOrDefaultAsync(g => g.Level == historicalLevel);

            if (historicalGrade == null)
            {
                // If the calculated level doesn't exist, fallback to enrollment grade
                return enrollmentGrade.Id;
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

        /// <summary>
        /// Gets the grade the student was enrolled in. If current grade is set, 
        /// calculates backwards to enrollment grade based on progression.
        /// </summary>
        private async Task<Grade> GetEnrollmentGradeAsync(Student student)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();
            
            // Calculate years progressed from enrollment to current
            int yearsFromEnrollmentToCurrent = CalculateYearsProgressed(
                student.EnrollmentTerm,
                student.EnrollmentYear,
                currentTerm,
                currentYear
            );

            // Calculate enrollment grade level by working backwards from current grade
            int enrollmentLevel = student.Grade.Level - yearsFromEnrollmentToCurrent;

            // Find the enrollment grade by level - ORDER BY Level for consistency
            var enrollmentGrade = await _context.Grades
                .OrderBy(g => g.Level)
                .FirstOrDefaultAsync(g => g.Level == enrollmentLevel);

            // If not found, return current grade as fallback
            return enrollmentGrade ?? student.Grade;
        }

        /// <summary>
        /// Calculates how many academic years a student has progressed between two terms.
        /// Only counts progression at the start of each academic year (Term 1).
        /// </summary>
        private int CalculateYearsProgressed(string fromTerm, int fromYear, string toTerm, int toYear)
        {
            // Years progressed is based on number of "Term 1" transitions
            // Students progress to next grade at the start of each year (Term 1)
            
            int yearsProgressed = 0;

            // If in same year, no progression
            if (fromYear == toYear)
                return 0;

            // Count number of Term 1 transitions
            // If enrolled in Term 2 or Term 3, they don't progress until next year's Term 1
            if (fromTerm == "Term 1")
            {
                // Started in Term 1, count all years including the starting year
                yearsProgressed = toYear - fromYear;
            }
            else
            {
                // Started in Term 2 or 3, don't count the enrollment year
                yearsProgressed = toYear - fromYear;
                
                // If we're still in the same academic cycle (before Term 1 of next year), subtract one
                if (CompareTerms(toTerm, "Term 1") < 0 && toYear == fromYear + 1)
                {
                    yearsProgressed = 0;
                }
            }

            return Math.Max(0, yearsProgressed);
        }

        private int CompareTerms(string term1, string term2)
        {
            var terms = new[] { "Term 1", "Term 2", "Term 3" };
            return Array.IndexOf(terms, term1) - Array.IndexOf(terms, term2);
        }
    }
}