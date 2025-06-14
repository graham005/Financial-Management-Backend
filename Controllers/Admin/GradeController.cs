using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class GradeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGrades()
        {
            var grades = await _context.Grades.ToListAsync();

            if(grades.Count == 0 || grades == null) 
                return NotFound("No Grades Found");

            var gradeDtos = grades.Select(grade => new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name,
            }).ToList();

            return Ok(gradeDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGradeById(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
                return NotFound("Grade with that ID not found");

            var gradeDto = new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name
            };

            return Ok(gradeDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGrade([FromBody] CreateGradeDto createGradeDto)
        {
            var grade = new Grade
            {
                Name = createGradeDto.Name,
            };

            await _context.Grades.AddAsync(grade);
            await _context.SaveChangesAsync();

            var gradeDto = new GradeDto
            {
                Id = grade.Id,
                Name = grade.Name,
            };

            return CreatedAtAction(nameof(GetGradeById), new { id = grade.Id }, gradeDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] CreateGradeDto updateGradeDto)
        {
            var grade = await _context.Grades.FindAsync(id);

            if (grade == null) return NotFound("Grade with that ID not found");

            grade.Name = updateGradeDto.Name;

            await _context.SaveChangesAsync();

            return Ok("Grade updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return NotFound("Grade with that ID not found");

            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();

            return Ok("Grade deleted successfully");
        }
    }
}
