using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class FeeStructureController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeeStructureController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeeStructure()
        {
            var feeStructure = await _context.FeeStructures
                .Include(fs => fs.Grade)
                .ToListAsync();

            if (feeStructure == null || feeStructure.Count == 0)
                return NotFound("No fee structure found");

            var feeStructureDtos = feeStructure.Select(fs => new FeeStructureDto
            {
                Id = fs.Id,
                GradeName = fs.Grade.Name,
                Term1Fee = fs.Term1Fee,
                Term2Fee = fs.Term2Fee,
                Term3Fee = fs.Term3Fee,
                TotalFee = fs.TotalFee
            }).ToList();

            return Ok(feeStructureDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeeStructureById(Guid id)
        {
            var feeStructure = await _context.FeeStructures
                .Include(fs => fs.Grade)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (feeStructure == null)
                return NotFound("No fee structure with that ID exists");

            var feeStructureDto = new FeeStructureDto
            {
                Id = feeStructure.Id,
                GradeName = feeStructure.Grade.Name,
                Term1Fee = feeStructure.Term1Fee,
                Term2Fee = feeStructure.Term2Fee,
                Term3Fee = feeStructure.Term3Fee,
                TotalFee = feeStructure.TotalFee
            };

            return Ok(feeStructureDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeeStructure([FromBody] CreateFeeStructureDto createFeeStructureDto)
        {
            // Find the Grade by NAme 
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Name == createFeeStructureDto.GradeName);
            if (grade == null)
                return NotFound("Grade not found.");

            // Check if a fee structure for this Grade already exists
            var existingFeeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(fs => fs.GradeId == grade.Id);

            if (existingFeeStructure != null)
                return Conflict("A fee structure for this grade already exists.");

            var feeStructure = new FeeStructure
            {
                GradeId = grade.Id,
                Term1Fee = createFeeStructureDto.Term1Fee,
                Term2Fee = createFeeStructureDto.Term2Fee,
                Term3Fee = createFeeStructureDto.Term3Fee,
            };

            await _context.FeeStructures.AddAsync(feeStructure);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFeeStructureById), new {id = feeStructure.Id}, feeStructure);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeeStructure(Guid id, [FromBody] CreateFeeStructureDto updateFeeStructureDto)
        {
            var feeStructure = await _context.FeeStructures.FindAsync(id);
            if (feeStructure == null) 
                return NotFound();

            // Find the Grade by NAme
            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Name == updateFeeStructureDto.GradeName);
            if (grade == null) 
                return NotFound("Grade with that ID not found");

            feeStructure.GradeId = grade.Id;
            feeStructure.Term1Fee = updateFeeStructureDto.Term1Fee;
            feeStructure.Term2Fee = updateFeeStructureDto.Term2Fee;
            feeStructure.Term3Fee = updateFeeStructureDto.Term3Fee;

            await _context.SaveChangesAsync();

            return Ok("FeeStructure updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeeStructure(Guid id)
        {
            var feeStructure = await _context.FeeStructures.FindAsync(id);

            if (feeStructure == null)
                return NotFound("Grade with that ID not found");

            _context.FeeStructures.Remove(feeStructure);
            await _context.SaveChangesAsync();

            return Ok("Fee Structure deleted successfully");
        }
    }
}
