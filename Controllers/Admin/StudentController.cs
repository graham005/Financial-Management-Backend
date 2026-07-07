using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Createstudent([FromBody] CreateStudentDto createStudentDto)
        {
            //Check if a grade exists
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Name == createStudentDto.GradeName);
            if (grade == null)
                return NotFound("Grade not found.");

            // Check if Parent exists, or to create a new one 
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Name == createStudentDto.ParentName);
            if (parent == null)
            {
                parent = new Models.Parent
                {
                    Name = createStudentDto.ParentName,
                    FirstName = createStudentDto.ParentFirstName,
                    LastName = createStudentDto.ParentLastName,
                    PhoneNumber = createStudentDto.ParentPhoneNumber
                };
                await _context.Parents.AddAsync(parent);
                await _context.SaveChangesAsync();
            }

            // Check if Admission Number already exists 
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.AdmissionNumber.ToLower() == createStudentDto.AdmissionNumber.ToLower());
            if (existingStudent != null)
                return Conflict("A Student with this addmission number already exists.");

            // Determine enrollment term and year
            string enrollmentTerm;
            int enrollmentYear;

            if (!string.IsNullOrWhiteSpace(createStudentDto.EnrollmentTerm) && createStudentDto.EnrollmentYear.HasValue && createStudentDto.EnrollmentYear.Value > 0)
            {
                enrollmentTerm = createStudentDto.EnrollmentTerm!;
                enrollmentYear = createStudentDto.EnrollmentYear.Value;
            }
            else
            {
                var today = DateTime.Today;
                if (today >= new DateTime(today.Year, 1, 1) && today < new DateTime(today.Year, 4, 1))
                    enrollmentTerm = "Term 1";
                else if (today >= new DateTime(today.Year, 5, 1) && today < new DateTime(today.Year, 8, 1))
                    enrollmentTerm = "Term 2";
                else if (today >= new DateTime(today.Year, 9, 1) && today < new DateTime(today.Year, 12, 1))
                    enrollmentTerm = "Term 3";
                else
                    enrollmentTerm = "Unknown";

                enrollmentYear = today.Year;
            }

            // Create the student
            var student = new Student
            {
                AdmissionNumber = createStudentDto.AdmissionNumber,
                Name = createStudentDto.Name,
                FirstName = createStudentDto.FirstName,
                MiddleName = createStudentDto.MiddleName,
                LastName = createStudentDto.LastName,
                Birthdate = createStudentDto.Birthdate,
                GradeId = grade.Id,
                ParentId = parent.Id,
                EnrollmentTerm = enrollmentTerm,
                EnrollmentYear = enrollmentYear
            };

            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudentById), new {id = student.Id}, student);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var totalStudents = await _context.Students.CountAsync();
                
                var students = await _context.Students
                   .Include(s => s.Grade)
                   .Include(s => s.Parent)
                   .Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync();

                var studentDtos = students.Select(student => new StudentDto
                {
                    Id = student.Id,
                    AdmissionNumber = student.AdmissionNumber ?? string.Empty,
                    Name = student.Name ?? string.Empty,
                    FirstName = student.FirstName ?? string.Empty,
                    MiddleName = student.MiddleName,
                    LastName = student.LastName ?? string.Empty,
                    Birthdate = student.Birthdate,
                    GradeName = student.Grade?.Name,
                    ParentName = student.Parent?.Name,
                    ParentFirstName = student.Parent?.FirstName,
                    ParentLastName = student.Parent?.LastName,
                    ParentPhoneNumber = student.Parent?.PhoneNumber,
                    Status = student.Status,
                    EnrollmentTerm = student.EnrollmentTerm,
                    EnrollmentYear = student.EnrollmentYear
                }).ToList();

                return Ok(new
                {
                    TotalCount = totalStudents,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalStudents / (double)pageSize),
                    Students = studentDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error retrieving students: {ex.Message}", Details = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Grade)
                    .Include(s => s.Parent)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                    return NotFound("Student with that ID not found");

                var studentDto = new StudentDto
                {
                    Id = student.Id,
                    AdmissionNumber = student.AdmissionNumber ?? string.Empty,
                    Name = student.Name ?? string.Empty,
                    FirstName = student.FirstName ?? string.Empty,
                    MiddleName = student.MiddleName,
                    LastName = student.LastName ?? string.Empty,
                    Birthdate = student.Birthdate,
                    GradeName = student.Grade?.Name,
                    ParentName = student.Parent?.Name,
                    ParentFirstName = student.Parent?.FirstName,
                    ParentLastName = student.Parent?.LastName,
                    ParentPhoneNumber = student.Parent?.PhoneNumber,
                    Status = student.Status,
                    EnrollmentTerm = student.EnrollmentTerm,
                    EnrollmentYear = student.EnrollmentYear
                };

                return Ok(studentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error retrieving student: {ex.Message}", Details = ex.InnerException?.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] CreateStudentDto updateStudentDto)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                    return NotFound("Student with that ID not found");

                // Check if Grade exists
                var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Name == updateStudentDto.GradeName);
                if (grade == null)
                    return NotFound("Grade not found.");

                // Check if parent exists, and adds a new one if none exist
                var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Name == updateStudentDto.ParentName);
                if (parent == null)
                {
                    parent = new Parent
                    {
                        Name = updateStudentDto.ParentName,
                        FirstName = updateStudentDto.ParentFirstName,
                        LastName = updateStudentDto.ParentLastName,
                        PhoneNumber = updateStudentDto.ParentPhoneNumber
                    };
                    await _context.Parents.AddAsync(parent);
                    await _context.SaveChangesAsync();
                }

                student.AdmissionNumber = updateStudentDto.AdmissionNumber;
                student.Name = updateStudentDto.Name;
                student.FirstName = updateStudentDto.FirstName;
                student.MiddleName = updateStudentDto.MiddleName;
                student.LastName = updateStudentDto.LastName;
                student.Birthdate = updateStudentDto.Birthdate;
                student.GradeId = grade.Id;
                student.ParentId = parent.Id;
                if (!string.IsNullOrWhiteSpace(updateStudentDto.EnrollmentTerm))
                    student.EnrollmentTerm = updateStudentDto.EnrollmentTerm;
                if (updateStudentDto.EnrollmentYear.HasValue && updateStudentDto.EnrollmentYear.Value > 0)
                    student.EnrollmentYear = updateStudentDto.EnrollmentYear.Value;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Student updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error updating student: {ex.Message}", Details = ex.InnerException?.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null) 
                    return NotFound("Student with that ID not found");

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error deleting student: {ex.Message}", Details = ex.InnerException?.Message });
            }
        }
    }
}
