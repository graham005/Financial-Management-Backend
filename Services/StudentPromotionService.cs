using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IStudentPromotionService
    {
        Task<PromotionPreviewDto> GetPromotionPreviewAsync();
        Task<PromotionResultDto> PromoteStudentsAsync(List<Guid> studentIds);
    }

    public class StudentPromotionService(
        ApplicationDbContext context,
        IAcademicTermService academicTermService) : IStudentPromotionService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IAcademicTermService _academicTermService = academicTermService;

        public async Task<PromotionPreviewDto> GetPromotionPreviewAsync()
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            if (currentTerm != "Term 3")
            {
                throw new InvalidOperationException("Promotions can only be done at the end of Term 3");
            }

            var students = await _context.Students
                .Include(s => s.Grade)
                .OrderBy(s => s.Grade.Level)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var groupedByGrade = students.GroupBy(s => s.Grade);
            var promotionGroups = new List<PromotionGroupDto>();

            foreach (var group in groupedByGrade)
            {
                var currentGrade = group.Key;
                var nextGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.Level == currentGrade.Level + 1);

                promotionGroups.Add(new PromotionGroupDto
                {
                    CurrentGradeId = currentGrade.Id,
                    CurrentGradeName = currentGrade.Name,
                    CurrentGradeLevel = currentGrade.Level,
                    NextGradeName = currentGrade.IsGraduationGrade ? "Graduated" : nextGrade?.Name,
                    NextGradeId = nextGrade?.Id,
                    IsGraduation = currentGrade.IsGraduationGrade,
                    Students = group.Select(s => new StudentPromotionDto
                    {
                        StudentId = s.Id,
                        AdmissionNumber = s.AdmissionNumber,
                        Name = s.Name
                    }).ToList()
                });
            }

            return new PromotionPreviewDto
            {
                AcademicYear = currentYear,
                Term = currentTerm,
                TotalStudents = students.Count,
                PromotionGroups = promotionGroups
            };
        }

        public async Task<PromotionResultDto> PromoteStudentsAsync(List<Guid> studentIds)
        {
            var (currentTerm, currentYear) = _academicTermService.GetCurrentAcademicTerm();

            if (currentTerm != "Term 3")
            {
                throw new InvalidOperationException("Promotions can only be done at the end of Term 3");
            }

            var result = new PromotionResultDto
            {
                PromotedStudents = new List<PromotedStudentDto>(),
                FailedPromotions = new List<FailedPromotionDto>()
            };

            foreach (var studentId in studentIds)
            {
                try
                {
                    var student = await _context.Students
                        .Include(s => s.Grade)
                        .FirstOrDefaultAsync(s => s.Id == studentId);

                    if (student == null)
                    {
                        result.FailedPromotions.Add(new FailedPromotionDto
                        {
                            StudentId = studentId,
                            Reason = "Student not found"
                        });
                        continue;
                    }

                    var oldGrade = student.Grade;

                    if (oldGrade.IsGraduationGrade)
                    {
                        result.PromotedStudents.Add(new PromotedStudentDto
                        {
                            StudentId = student.Id,
                            StudentName = student.Name,
                            FromGrade = oldGrade.Name,
                            ToGrade = "Graduated",
                            IsGraduation = true
                        });
                        // Note: We don't change GradeId for graduated students
                        // They stay in their graduation grade
                    }
                    else
                    {
                        var nextGrade = await _context.Grades
                            .FirstOrDefaultAsync(g => g.Level == oldGrade.Level + 1);

                        if (nextGrade == null)
                        {
                            result.FailedPromotions.Add(new FailedPromotionDto
                            {
                                StudentId = studentId,
                                Reason = "Next grade not found"
                            });
                            continue;
                        }

                        student.GradeId = nextGrade.Id;

                        result.PromotedStudents.Add(new PromotedStudentDto
                        {
                            StudentId = student.Id,
                            StudentName = student.Name,
                            FromGrade = oldGrade.Name,
                            ToGrade = nextGrade.Name,
                            IsGraduation = false
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedPromotions.Add(new FailedPromotionDto
                    {
                        StudentId = studentId,
                        Reason = ex.Message
                    });
                }
            }

            if (result.PromotedStudents.Any())
            {
                await _context.SaveChangesAsync();
            }

            return result;
        }
    }

    // DTOs
    public class PromotionPreviewDto
    {
        public int AcademicYear { get; set; }
        public string Term { get; set; }
        public int TotalStudents { get; set; }
        public List<PromotionGroupDto> PromotionGroups { get; set; }
    }

    public class PromotionGroupDto
    {
        public Guid CurrentGradeId { get; set; }
        public string CurrentGradeName { get; set; }
        public int CurrentGradeLevel { get; set; }
        public string NextGradeName { get; set; }
        public Guid? NextGradeId { get; set; }
        public bool IsGraduation { get; set; }
        public List<StudentPromotionDto> Students { get; set; }
    }

    public class StudentPromotionDto
    {
        public Guid StudentId { get; set; }
        public string AdmissionNumber { get; set; }
        public string Name { get; set; }
    }

    public class PromotionResultDto
    {
        public List<PromotedStudentDto> PromotedStudents { get; set; }
        public List<FailedPromotionDto> FailedPromotions { get; set; }
    }

    public class PromotedStudentDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string FromGrade { get; set; }
        public string ToGrade { get; set; }
        public bool IsGraduation { get; set; }
    }

    public class FailedPromotionDto
    {
        public Guid StudentId { get; set; }
        public string Reason { get; set; }
    }
}