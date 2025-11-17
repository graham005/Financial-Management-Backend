using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Data
{
    public static class GradeSeeder
    {
        public static async Task UpdateGradeLevels(ApplicationDbContext context)
        {
            // Define the correct grade order and levels
            var gradeMapping = new Dictionary<string, (int Level, string Category, bool IsGraduation)>
            {
                { "PP1", (1, "Pre-Primary", false) },
                { "PP2", (2, "Pre-Primary", false) },
                { "Grade 1", (3, "Primary", false) },
                { "Grade 2", (4, "Primary", false) },
                { "Grade 3", (5, "Primary", false) },
                { "Grade 4", (6, "Primary", false) },
                { "Grade 5", (7, "Primary", false) },
                { "Grade 6", (8, "Primary", true) } // Graduation grade
            };

            foreach (var mapping in gradeMapping)
            {
                var grade = await context.Grades
                    .FirstOrDefaultAsync(g => g.Name.ToLower() == mapping.Key.ToLower());

                if (grade != null)
                {
                    grade.Level = mapping.Value.Level;
                    grade.Category = mapping.Value.Category;
                    grade.IsGraduationGrade = mapping.Value.IsGraduation;
                }
                else
                {
                    // Create the grade if it doesn't exist
                    var newGrade = new Grade
                    {
                        Name = mapping.Key,
                        Level = mapping.Value.Level,
                        Category = mapping.Value.Category,
                        IsGraduationGrade = mapping.Value.IsGraduation
                    };
                    await context.Grades.AddAsync(newGrade);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}