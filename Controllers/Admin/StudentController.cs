using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
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
                    PhoneNumber = createStudentDto.ParentPhoneNumber
                };
                await _context.Parents.AddAsync(parent);
                await _context.SaveChangesAsync();
            }

            // Check if Admission Number already exists 
            var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.AdmissionNumber.Equals (createStudentDto.AdmissionNumber,
                StringComparison.OrdinalIgnoreCase));
            if (existingStudent != null)
                return Conflict("A Student with this addmission number already exists.");

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
            };

            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudentById), new {id = student.Id}, student);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var students = await _context.Students
               .Include(s => s.Grade)
               .Include(s => s.Parent)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            if(students == null || students.Count == 0)
            {
                return NoContent();
            }

            var studentDtos = students.Select(student => new StudentDto
            {
                Id = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                Name = student.Name,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Birthdate = student.Birthdate,
                GradeName = student.Grade?.Name,
                ParentName = student.Parent.Name,
                ParentFirstName = student.Parent.FirstName,
                ParentLastName = student.Parent.LastName,
                ParentPhoneNumber = student.Parent?.PhoneNumber

            }).ToList();

            return Ok(studentDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Grade)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var studentDto = new StudentDto
            {
                Id = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                Name = student.Name,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Birthdate = student.Birthdate,
                GradeName = student.Grade.Name,
                ParentName = student.Parent.Name,
                ParentFirstName = student.Parent.FirstName,
                ParentLastName = student.Parent.LastName,
                ParentPhoneNumber = student.Parent.PhoneNumber
            };

            return Ok(studentDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] CreateStudentDto updateStudentDto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

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
                    PhoneNumber = updateStudentDto.ParentPhoneNumber,
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

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
