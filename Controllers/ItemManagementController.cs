using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
using Financial_management_backend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ItemManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------- Required Items Methods (Admin) -----------

        [HttpGet("required-items")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> GetAllRequiredItems()
        {
            try
            {
                var items = await _context.RequiredItems.ToListAsync();

                var result = items.Select(ri => new RequiredItemDto
                {
                    Id = ri.Id,
                    ItemName = ri.ItemName,
                    ExpectedQuantity = ri.ExpectedQuantity,
                    Unit = ri.Unit
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("required-items/{id}")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> GetRequiredItemById(Guid id)
        {
            try
            {
                var item = await _context.RequiredItems.FindAsync(id);
                if (item == null)
                    return NotFound("Required item not found.");

                var dto = new RequiredItemDto
                {
                    Id = item.Id,
                    ItemName = item.ItemName,
                    ExpectedQuantity = item.ExpectedQuantity,
                    Unit = item.Unit
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("required-items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRequiredItem([FromBody] CreateRequiredItemDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check for duplicate items for the same student/grade
                var existingItem = await _context.RequiredItems
                    .FirstOrDefaultAsync(ri => ri.ItemName == dto.ItemName);
                                            

                if (existingItem != null)
                    return Conflict("A required item with this name already exists for the specified student/grade.");

                var requiredItem = new RequiredItem
                {
                    ItemName = dto.ItemName,
                    ExpectedQuantity = dto.ExpectedQuantity,
                    Unit = dto.Unit,
                };

                await _context.RequiredItems.AddAsync(requiredItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRequiredItemById), new { id = requiredItem.Id }, requiredItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("required-items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRequiredItem(Guid id, [FromBody] UpdateRequiredItemDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var item = await _context.RequiredItems.FindAsync(id);
                if (item == null)
                    return NotFound("Required item not found.");

                if (!string.IsNullOrWhiteSpace(dto.ItemName))
                    item.ItemName = dto.ItemName;
                if (dto.ExpectedQuantity.HasValue)
                    item.ExpectedQuantity = dto.ExpectedQuantity.Value;
                if (!string.IsNullOrWhiteSpace(dto.Unit))
                    item.Unit = dto.Unit;

                await _context.SaveChangesAsync();
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("required-items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRequiredItem(Guid id)
        {
            try
            {
                var item = await _context.RequiredItems.FindAsync(id);
                if (item == null)
                    return NotFound("Required item not found.");

                // Check if there are any received items linked to this required item
                var hasReceivedItems = await _context.ItemsReceived
                    .AnyAsync(ir => ir.RequiredItemId == id);

                if (hasReceivedItems)
                    return BadRequest("Cannot delete required item as it has associated received items.");

                _context.RequiredItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok("Required item deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ----------- Items Received Methods (Accountant/StockManager) -----------

        [HttpGet("items-received")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> GetAllItemsReceived()
        {
            try
            {
                var items = await _context.ItemsReceived
                    .Include(ir => ir.RequiredItem)
                    .Include(ir => ir.Student)
                    .ToListAsync();

                var result = items.Select(ir => new ItemReceivedDto
                {
                    Id = ir.Id,
                    RequiredItemId = ir.RequiredItemId,
                    ItemName = ir.RequiredItem?.ItemName ?? string.Empty,
                    Unit = ir.RequiredItem?.Unit ?? string.Empty,
                    ExpectedQuantity = ir.RequiredItem?.ExpectedQuantity ?? 0,
                    StudentId = ir.StudentId,
                    StudentName = ir.Student.Name,
                    Quantity = ir.Quantity,
                    DateReceived = ir.DateReceived,
                    RecordedBy = ir.RecordedBy,
                    VarianceQuantity = ir.Quantity - (ir.RequiredItem?.ExpectedQuantity ?? 0)
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("items-received/{id}")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> GetItemReceivedById(Guid id)
        {
            try
            {
                var item = await _context.ItemsReceived
                    .Include(ir => ir.RequiredItem)
                    .Include(ir => ir.Student)
                    .FirstOrDefaultAsync(ir => ir.Id == id);

                if (item == null)
                    return NotFound("Item received record not found.");

                var dto = new ItemReceivedDto
                {
                    Id = item.Id,
                    RequiredItemId = item.RequiredItemId,
                    ItemName = item.RequiredItem?.ItemName ?? string.Empty,
                    Unit = item.RequiredItem?.Unit ?? string.Empty,
                    ExpectedQuantity = item.RequiredItem?.ExpectedQuantity ?? 0,
                    StudentId = item.StudentId,
                    StudentName = item.Student.Name,
                    Quantity = item.Quantity,
                    DateReceived = item.DateReceived,
                    RecordedBy = item.RecordedBy,
                    VarianceQuantity = item.Quantity - (item.RequiredItem?.ExpectedQuantity ?? 0)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("items-received")]
        [Authorize(Roles = "Accountant,StockManager")]
        public async Task<IActionResult> CreateItemReceived([FromBody] CreateItemReceivedDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Validate required item exists
                var requiredItem = await _context.RequiredItems.FindAsync(dto.RequiredItemId);
                if (requiredItem == null)
                    return NotFound("Required item not found.");
               

                var itemReceived = new ItemReceived
                {
                    RequiredItemId = dto.RequiredItemId,
                    StudentId = dto.StudentId,
                    Quantity = dto.Quantity,
                    DateReceived = dto.DateReceived,
                    RecordedBy = (Guid)userId
                };

                await _context.ItemsReceived.AddAsync(itemReceived);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetItemReceivedById), new { id = itemReceived.Id }, itemReceived);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("items-received/{id}")]
        [Authorize(Roles = "Accountant,StockManager")]
        public async Task<IActionResult> UpdateItemReceived(Guid id, [FromBody] UpdateItemReceivedDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var item = await _context.ItemsReceived.FindAsync(id);
                if (item == null)
                    return NotFound("Item received record not found.");

                // Validate required item if provided
                if (dto.RequiredItemId.HasValue)
                {
                    var requiredItem = await _context.RequiredItems.FindAsync(dto.RequiredItemId.Value);
                    if (requiredItem == null)
                        return BadRequest("Required item not found.");
                    item.RequiredItemId = dto.RequiredItemId.Value;
                }

                if (dto.Quantity.HasValue)
                    item.Quantity = dto.Quantity.Value;
                if (dto.DateReceived.HasValue)
                    item.DateReceived = dto.DateReceived.Value;

                await _context.SaveChangesAsync();
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("items-received/{id}")]
        [Authorize(Roles = "Accountant,StockManager")]
        public async Task<IActionResult> DeleteItemReceived(Guid id)
        {
            try
            {
                var item = await _context.ItemsReceived.FindAsync(id);
                if (item == null)
                    return NotFound("Item received record not found.");

                _context.ItemsReceived.Remove(item);
                await _context.SaveChangesAsync();

                return Ok("Item received record deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}