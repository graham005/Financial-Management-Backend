using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Financial_management_backend.Services.Dtos;
using Financial_management_backend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class OtherFeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAcademicTermService _academicTermService;

        public OtherFeeController(
            ApplicationDbContext context,
            IAcademicTermService academicTermService)
        {
            _context = context;
            _academicTermService = academicTermService;
        }

        // GET: api/admin/otherfee
        [HttpGet]
        public async Task<IActionResult> GetAllOtherFees(
            [FromQuery] int? year,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.OtherFees.AsQueryable();

                if (year.HasValue)
                    query = query.Where(of => of.AcademicYear == year.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(of => of.Status == status);

                var fees = await query
                    .OrderByDescending(of => of.AcademicYear)
                    .ThenBy(of => of.Name)
                    .ToListAsync();

                var result = fees.Select(of => new OtherFeeDto
                {
                    Id = of.Id,
                    Name = of.Name,
                    Description = of.Description,
                    Amount = of.Amount,
                    AcademicYear = of.AcademicYear,
                    Status = of.Status,
                    CreatedAt = of.CreatedAt,
                    ArchivedAt = of.ArchivedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving other fees: {ex.Message}");
            }
        }

        // GET: api/admin/otherfee/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOtherFeeById(Guid id)
        {
            try
            {
                var fee = await _context.OtherFees.FindAsync(id);

                if (fee == null)
                    return NotFound("Other fee not found");

                return Ok(new OtherFeeDto
                {
                    Id = fee.Id,
                    Name = fee.Name,
                    Description = fee.Description,
                    Amount = fee.Amount,
                    AcademicYear = fee.AcademicYear,
                    Status = fee.Status,
                    CreatedAt = fee.CreatedAt,
                    ArchivedAt = fee.ArchivedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving other fee: {ex.Message}");
            }
        }

        // POST: api/admin/otherfee
        [HttpPost]
        public async Task<IActionResult> CreateOtherFee([FromBody] CreateOtherFeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                // Use provided year or current academic year
                var (_, currentYear) = _academicTermService.GetCurrentAcademicTerm();
                var academicYear = dto.AcademicYear ?? currentYear;

                // Check for duplicate active fees with same name for this year
                var existingFee = await _context.OtherFees
                    .FirstOrDefaultAsync(of => of.Name == dto.Name && 
                                             of.AcademicYear == academicYear &&
                                             of.Status == "Active");

                if (existingFee != null)
                    return Conflict($"An active fee named '{dto.Name}' already exists for year {academicYear}");

                var otherFee = new OtherFee
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Amount = dto.Amount,
                    AcademicYear = academicYear,
                    Status = "Active",
                    CreatedBy = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.OtherFees.AddAsync(otherFee);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOtherFeeById), new { id = otherFee.Id }, new OtherFeeDto
                {
                    Id = otherFee.Id,
                    Name = otherFee.Name,
                    Description = otherFee.Description,
                    Amount = otherFee.Amount,
                    AcademicYear = otherFee.AcademicYear,
                    Status = otherFee.Status,
                    CreatedAt = otherFee.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating other fee: {ex.Message}");
            }
        }

        // PATCH: api/admin/otherfee/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateOtherFee(Guid id, [FromBody] UpdateOtherFeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var fee = await _context.OtherFees.FindAsync(id);
                if (fee == null)
                    return NotFound("Other fee not found");

                if (fee.Status == "Archived")
                    return BadRequest("Cannot update archived fees");

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    // Check for duplicate names for the same year
                    var duplicate = await _context.OtherFees
                        .AnyAsync(of => of.Name == dto.Name && 
                                      of.AcademicYear == fee.AcademicYear &&
                                      of.Id != id &&
                                      of.Status == "Active");

                    if (duplicate)
                        return Conflict($"An active fee named '{dto.Name}' already exists for year {fee.AcademicYear}");

                    fee.Name = dto.Name;
                }

                if (!string.IsNullOrEmpty(dto.Description))
                    fee.Description = dto.Description;

                if (dto.Amount > 0)
                    fee.Amount = dto.Amount;

                await _context.SaveChangesAsync();

                return Ok(new OtherFeeDto
                {
                    Id = fee.Id,
                    Name = fee.Name,
                    Description = fee.Description,
                    Amount = fee.Amount,
                    AcademicYear = fee.AcademicYear,
                    Status = fee.Status,
                    CreatedAt = fee.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating other fee: {ex.Message}");
            }
        }

        // POST: api/admin/otherfee/archive
        [HttpPost("archive")]
        public async Task<IActionResult> ArchiveOtherFees([FromBody] ArchiveOtherFeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var query = _context.OtherFees
                    .Where(of => of.AcademicYear == dto.AcademicYear && of.Status == "Active");

                // If specific fee IDs provided, filter to those
                if (dto.FeeIds != null && dto.FeeIds.Any())
                    query = query.Where(of => dto.FeeIds.Contains(of.Id));

                var feesToArchive = await query.ToListAsync();

                if (!feesToArchive.Any())
                    return NotFound($"No active fees found for year {dto.AcademicYear}");

                foreach (var fee in feesToArchive)
                {
                    fee.Status = "Archived";
                    fee.ArchivedAt = DateTime.UtcNow;
                    fee.ArchivedBy = userId.Value;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Successfully archived {feesToArchive.Count} fee(s) for year {dto.AcademicYear}",
                    ArchivedCount = feesToArchive.Count,
                    ArchivedFees = feesToArchive.Select(f => new { f.Id, f.Name, f.Amount })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error archiving fees: {ex.Message}");
            }
        }

        // POST: api/admin/otherfee/{id}/unarchive
        [HttpPost("{id}/unarchive")]
        public async Task<IActionResult> UnarchiveOtherFee(Guid id)
        {
            try
            {
                var fee = await _context.OtherFees.FindAsync(id);
                if (fee == null)
                    return NotFound("Other fee not found");

                if (fee.Status != "Archived")
                    return BadRequest("Fee is not archived");

                // Check for duplicate active fees with same name for this year
                var duplicate = await _context.OtherFees
                    .AnyAsync(of => of.Name == fee.Name && 
                                  of.AcademicYear == fee.AcademicYear &&
                                  of.Id != id &&
                                  of.Status == "Active");

                if (duplicate)
                    return Conflict($"An active fee named '{fee.Name}' already exists for year {fee.AcademicYear}");

                fee.Status = "Active";
                fee.ArchivedAt = null;
                fee.ArchivedBy = null;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Fee unarchived successfully",
                    Fee = new OtherFeeDto
                    {
                        Id = fee.Id,
                        Name = fee.Name,
                        Description = fee.Description,
                        Amount = fee.Amount,
                        AcademicYear = fee.AcademicYear,
                        Status = fee.Status,
                        CreatedAt = fee.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unarchiving fee: {ex.Message}");
            }
        }

        // DELETE: api/admin/otherfee/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOtherFee(Guid id)
        {
            try
            {
                var fee = await _context.OtherFees.FindAsync(id);
                if (fee == null)
                    return NotFound("Other fee not found");

                // Check if any payments reference this fee
                var hasPayments = await _context.FeePayments
                    .AnyAsync(fp => fp.FeeId == id && fp.FeeSource == "OtherFee");

                if (hasPayments)
                    return BadRequest("Cannot delete fee with associated payments. Consider archiving instead.");

                _context.OtherFees.Remove(fee);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Other fee deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting other fee: {ex.Message}");
            }
        }
    }
}
