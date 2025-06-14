using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class OtherFeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OtherFeeController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpPost]
        public async Task<IActionResult> CreateOtherFee([FromBody] CreateOtherFeeDto createOtherFeeDto)
        {
            var grade = await _context.Grades.FindAsync(createOtherFeeDto.GradeId);
            if (grade == null)
            {
                return NotFound("Grade not found.");
            }

            var otherFee = new OtherFee
            {
                Name = createOtherFeeDto.Name,
                GradeId = createOtherFeeDto.GradeId,
                Amount = createOtherFeeDto.Amount
            };

            await _context.OtherFees.AddAsync(otherFee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOtherFeeById), new { id = otherFee.Id }, otherFee);

        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateOtherFee(Guid id, [FromBody] UpdateOtherFeeDto updateOtherFeeDto)
        {
            var otherFee = await _context.OtherFees.FindAsync(id);
            if (otherFee == null)
                return NotFound();

            var grade = await _context.OtherFees.FindAsync(updateOtherFeeDto.GradeId);
            if(grade == null)
                return NotFound("Grade not found");

            if (updateOtherFeeDto.Name != null)
                otherFee.Name = updateOtherFeeDto.Name;

            if (updateOtherFeeDto.GradeId != null)
            {
                var gradeID = await _context.Grades.FindAsync(updateOtherFeeDto.GradeId);
                if (gradeID == null)
                    return NotFound("Grade not found");
                otherFee.GradeId = updateOtherFeeDto.GradeId.Value;
            }

            if (updateOtherFeeDto.Amount != null)
                otherFee.Amount = updateOtherFeeDto.Amount.Value;

            await _context.SaveChangesAsync();
            return Ok(otherFee);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOtherFees([FromQuery]int? gradeId)
        {
            var query = _context.OtherFees
                .Include(of => of.Grade) // Include grade details
                .AsQueryable();

            if(gradeId == null)
            {
                return NotFound("Grade with that ID not found");
            }
            if(gradeId.HasValue)
            {
                query = query.Where(of => of.GradeId == gradeId);
            }

            var otherFees = await query.ToListAsync();

            return Ok(otherFees.Select(of => new
            {
                Id = of.Id,
                Name = of.Name,
                GradeName = of.Grade.Name,
                Amount = of.Amount
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOtherFeeById(Guid id)
        {
            var otherFee = await _context.OtherFees
                .Include(of => of.Grade) // include Grade details
                .FirstOrDefaultAsync(of => of.Id == id);

            if (otherFee == null)
                return NotFound("Other with that ID not found.");

            return Ok(new
            {
                Id = otherFee.Id,
                Name = otherFee.Name,
                GradeName = otherFee.Grade.Name,
                Amount = otherFee.Amount
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOtherFee(Guid id)
        {
            var otherFee = await _context.OtherFees.FindAsync(id);
            if (otherFee == null)
                return NotFound("OtherFee with that ID not found");

            _context.OtherFees.Remove(otherFee);
            await _context.SaveChangesAsync();
            return Ok("The Other Fee has been deleted Successfully");
        }
    }
}
