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
                    Unit = ri.Unit,
                    ApproximateValue = ri.ApproximateValue
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
                    Unit = item.Unit,
                    ApproximateValue = item.ApproximateValue
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
                    ApproximateValue = dto.ApproximateValue
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
                if (dto.ApproximateValue.HasValue)
                    item.ApproximateValue = dto.ApproximateValue.Value;

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
                    VarianceQuantity = ir.Quantity - (ir.RequiredItem?.ExpectedQuantity ?? 0),
                    Term = ir.Term,
                    Year = ir.Year,
                    IsMonetaryContribution = ir.IsMonetaryContribution,
                    ValueAtTimeOfContribution = ir.ValueAtTimeOfContribution,
                    ApproximateValue = ir.RequiredItem?.ApproximateValue ?? 0
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
                    VarianceQuantity = item.Quantity - (item.RequiredItem?.ExpectedQuantity ?? 0),
                    Term = item.Term,
                    Year = item.Year,
                    IsMonetaryContribution = item.IsMonetaryContribution,
                    ValueAtTimeOfContribution = item.ValueAtTimeOfContribution,
                    ApproximateValue = item.RequiredItem?.ApproximateValue ?? 0
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
               
                decimal quantity = dto.Quantity;
                decimal valueAtTimeOfContribution = 0;

                // If it's a monetary contribution, calculate equivalent item quantity
                if (dto.IsMonetaryContribution && requiredItem.ApproximateValue > 0)
                {
                    valueAtTimeOfContribution = dto.Quantity; // The quantity is actually money amount
                    quantity = Math.Round(dto.Quantity / requiredItem.ApproximateValue, 2); // Convert money to item quantity
                }

                var itemReceived = new ItemReceived
                {
                    RequiredItemId = dto.RequiredItemId,
                    StudentId = dto.StudentId,
                    Quantity = quantity,
                    DateReceived = dto.DateReceived,
                    RecordedBy = (Guid)userId,
                    Term = dto.Term,
                    Year = dto.Year,
                    IsMonetaryContribution = dto.IsMonetaryContribution,
                    ValueAtTimeOfContribution = valueAtTimeOfContribution
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

        // In UpdateItemReceived method, fix CS8600 by using nullable RequiredItem and null checks

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

                RequiredItem? requiredItem = null;

                // Validate required item if provided
                if (dto.RequiredItemId.HasValue)
                {
                    requiredItem = await _context.RequiredItems.FindAsync(dto.RequiredItemId.Value);
                    if (requiredItem == null)
                        return BadRequest("Required item not found.");
                    item.RequiredItemId = dto.RequiredItemId.Value;
                }
                else if (dto.IsMonetaryContribution.HasValue && dto.IsMonetaryContribution.Value != item.IsMonetaryContribution)
                {
                    // We need the required item if changing monetary contribution status
                    requiredItem = await _context.RequiredItems.FindAsync(item.RequiredItemId);
                    if (requiredItem == null)
                        return BadRequest("Required item information is missing.");
                }

                // If changing monetary contribution status, recalculate the quantity
                if (dto.IsMonetaryContribution.HasValue && dto.IsMonetaryContribution.Value != item.IsMonetaryContribution)
                {
                    // Switching from monetary to physical or vice versa
                    if (dto.IsMonetaryContribution.Value)
                    {
                        // Switching to monetary - original quantity becomes value, calculate new quantity
                        if (requiredItem == null)
                            return BadRequest("Required item information is missing.");
                        item.ValueAtTimeOfContribution = item.Quantity;
                        item.Quantity = Math.Round(item.Quantity / requiredItem.ApproximateValue, 2);
                    }
                    else
                    {
                        // Switching to physical - value becomes quantity
                        item.Quantity = item.ValueAtTimeOfContribution;
                        item.ValueAtTimeOfContribution = 0;
                    }
                    item.IsMonetaryContribution = dto.IsMonetaryContribution.Value;
                }

                // Update quantity if provided
                if (dto.Quantity.HasValue)
                {
                    if (dto.IsMonetaryContribution.HasValue && dto.IsMonetaryContribution.Value)
                    {
                        // It's a monetary contribution - store the money value and calculate quantity
                        if (requiredItem == null)
                            requiredItem = await _context.RequiredItems.FindAsync(item.RequiredItemId);
                        if (requiredItem == null)
                            return BadRequest("Required item information is missing.");
                        item.ValueAtTimeOfContribution = dto.Quantity.Value;
                        item.Quantity = Math.Round(dto.Quantity.Value / requiredItem.ApproximateValue, 2);
                    }
                    else if (item.IsMonetaryContribution)
                    {
                        // Existing record is monetary but no change in status - update value and quantity
                        if (requiredItem == null)
                            requiredItem = await _context.RequiredItems.FindAsync(item.RequiredItemId);
                        if (requiredItem == null)
                            return BadRequest("Required item information is missing.");
                        item.ValueAtTimeOfContribution = dto.Quantity.Value;
                        item.Quantity = Math.Round(dto.Quantity.Value / requiredItem.ApproximateValue, 2);
                    }
                    else
                    {
                        // Physical contribution - just update quantity
                        item.Quantity = dto.Quantity.Value;
                    }
                }

                if (dto.DateReceived.HasValue)
                    item.DateReceived = dto.DateReceived.Value;

                if (!string.IsNullOrWhiteSpace(dto.Term))
                    item.Term = dto.Term;

                if (dto.Year.HasValue)
                    item.Year = dto.Year.Value;

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

        // Add this endpoint to simplify item contributions
        [HttpPost("record-contribution")]
        [Authorize(Roles = "Accountant,StockManager")]
        public async Task<IActionResult> RecordItemContribution([FromBody] ItemContributionDto contributionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                var student = await _context.Students.FindAsync(contributionDto.StudentId);
                if (student == null)
                    return NotFound("Student not found.");

                // Create a list to store created items
                var createdItems = new List<ItemReceived>();

                // Process each item allocation
                foreach (var allocation in contributionDto.ItemAllocations)
                {
                    // Validate required item exists
                    var requiredItem = await _context.RequiredItems.FindAsync(allocation.RequiredItemId);
                    if (requiredItem == null)
                        return BadRequest($"Required item with ID {allocation.RequiredItemId} not found.");
               
                    decimal quantity = allocation.Quantity;
                    decimal valueAtTimeOfContribution = 0;

                    // If it's a monetary contribution, calculate equivalent item quantity
                    if (allocation.IsMonetaryContribution && requiredItem.ApproximateValue > 0)
                    {
                        valueAtTimeOfContribution = allocation.Quantity; // The quantity is actually money amount
                        quantity = Math.Round(allocation.Quantity / requiredItem.ApproximateValue, 2); // Convert money to item quantity
                    }

                    var itemReceived = new ItemReceived
                    {
                        RequiredItemId = allocation.RequiredItemId,
                        StudentId = contributionDto.StudentId,
                        Quantity = quantity,
                        DateReceived = contributionDto.DateReceived,
                        RecordedBy = (Guid)userId,
                        Term = contributionDto.Term,
                        Year = contributionDto.Year,
                        IsMonetaryContribution = allocation.IsMonetaryContribution,
                        ValueAtTimeOfContribution = valueAtTimeOfContribution
                    };

                    await _context.ItemsReceived.AddAsync(itemReceived);
                    createdItems.Add(itemReceived);
                }

                await _context.SaveChangesAsync();

                // Format a nice response
                return Ok(new
                {
                    Message = "Item contributions recorded successfully",
                    StudentId = contributionDto.StudentId,
                    StudentName = student.Name,
                    DateReceived = contributionDto.DateReceived,
                    Term = contributionDto.Term,
                    Year = contributionDto.Year,
                    Items = createdItems.Select(item => new
                    {
                        Id = item.Id,
                        RequiredItemId = item.RequiredItemId,
                        Quantity = item.Quantity,
                        IsMonetaryContribution = item.IsMonetaryContribution,
                        MonetaryValue = item.ValueAtTimeOfContribution
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Format the existing GetPendingItemObligations to return a more structured DTO
        [HttpGet("student/{studentId}/required-items")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> GetRequiredItemsForStudent(Guid studentId, string? term = null, int? year = null)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound("Student not found.");

                // Get all required items
                var requiredItems = await _context.RequiredItems.ToListAsync();
                
                // Create a list for available item requirements
                var pendingItems = new List<AvailableItemRequirementDto>();
                
                // Get current term/year if not specified
                if (string.IsNullOrEmpty(term) || !year.HasValue)
                {
                    var (currentTerm, currentYear) = GetCurrentAcademicTerm();
                    term = term ?? currentTerm;
                    year = year ?? currentYear;
                }
                
                foreach (var requiredItem in requiredItems)
                {
                    // Query to get all items received by this student for this required item
                    var receivedItemsQuery = _context.ItemsReceived
                        .Where(ir => ir.StudentId == studentId && ir.RequiredItemId == requiredItem.Id);
                        
                    // Apply term/year filter if provided
                    if (!string.IsNullOrEmpty(term))
                        receivedItemsQuery = receivedItemsQuery.Where(ir => ir.Term == term);
                        
                    if (year.HasValue)
                        receivedItemsQuery = receivedItemsQuery.Where(ir => ir.Year == year.Value);
                        
                    // Get total received quantity
                    var totalReceived = await receivedItemsQuery.SumAsync(ir => ir.Quantity);
                    
                    // Calculate pending quantity
                    var pendingQuantity = requiredItem.ExpectedQuantity - totalReceived;
                    
                    // Add to result even if fully contributed (just show 0 pending)
                    pendingItems.Add(new AvailableItemRequirementDto
                    {
                        RequiredItemId = requiredItem.Id,
                        ItemName = requiredItem.ItemName,
                        Unit = requiredItem.Unit,
                        ExpectedQuantity = requiredItem.ExpectedQuantity,
                        ReceivedQuantity = totalReceived,
                        PendingQuantity = Math.Max(pendingQuantity, 0),
                        ApproximateValue = requiredItem.ApproximateValue,
                        MonetaryEquivalent = Math.Max(pendingQuantity, 0) * requiredItem.ApproximateValue,
                        Term = term,
                        Year = year.Value,
                        IsPastDue = IsPastDue(term, year.Value)
                    });
                }
                
                return Ok(new AvailableItemRequirementsDto
                {
                    StudentId = studentId,
                    StudentName = student.Name,
                    PendingItems = pendingItems,
                    TotalPendingItems = pendingItems.Count(i => i.PendingQuantity > 0),
                    HasPendingRequirements = pendingItems.Any(i => i.PendingQuantity > 0)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to determine if a term is past due
        private bool IsPastDue(string term, int year)
        {
            var (currentTerm, currentYear) = GetCurrentAcademicTerm();
            
            // If the year is in the past, it's past due
            if (year < currentYear) return true;
            
            // If it's the current year, check the term
            if (year == currentYear)
            {
                var terms = new[] { "Term 1", "Term 2", "Term 3" };
                return Array.IndexOf(terms, term) < Array.IndexOf(terms, currentTerm);
            }
            
            // If it's a future year, it's not past due
            return false;
        }

        // Helper method to get current academic term
        private (string Term, int Year) GetCurrentAcademicTerm()
        {
            // You could inject IAcademicTermService instead of reimplementing this
            var now = DateTime.Now;
            int year = now.Year;
            string term;
            
            if (now.Month <= 4) term = "Term 1";
            else if (now.Month <= 8) term = "Term 2";
            else term = "Term 3";
            
            return (term, year);
        }
    }
}