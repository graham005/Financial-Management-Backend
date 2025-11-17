using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Models.ItemManagement;
using Financial_management_backend.Services;
using Financial_management_backend.Services.Dtos.ItemManagement;
using Financial_management_backend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemLedgerController(
        ApplicationDbContext context,
        IAcademicTermService academicTermService) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IAcademicTermService _academicTermService = academicTermService;

        // ----------- Requirement Lists Methods -----------

        [HttpGet("requirement-lists")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> GetAllRequirementLists()
        {
            try
            {
                var lists = await _context.RequirementLists
                    .Include(rl => rl.Creator)
                    .OrderByDescending(rl => rl.AcademicYear)
                    .ThenBy(rl => rl.Term)
                    .ToListAsync();

                var result = lists.Select(rl => new
                {
                    rl.Id,
                    rl.Term,
                    rl.AcademicYear,
                    rl.CreatedAt,
                    CreatedBy = rl.Creator.Username,
                    rl.Status,
                    ItemCount = rl.Items.Count
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("requirement-lists/{id}")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> GetRequirementListById(Guid id)
        {
            try
            {
                var list = await _context.RequirementLists
                    .Include(rl => rl.Creator)
                    .Include(rl => rl.Items)
                    .FirstOrDefaultAsync(rl => rl.Id == id);

                if (list == null)
                    return NotFound("Requirement list not found.");

                var result = new RequirementListDetailDto
                {
                    Id = list.Id,
                    Term = list.Term,
                    AcademicYear = list.AcademicYear,
                    CreatedAt = list.CreatedAt,
                    CreatedBy = list.Creator.Username,
                    Status = list.Status,
                    Items = [.. list.Items.Select(item => new RequirementItemDto
                    {
                        Id = item.Id,
                        ItemName = item.ItemName,
                        RequiredQuantity = item.RequiredQuantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Description = item.Description
                    })]
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("requirement-lists")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRequirementList([FromBody] CreateRequirementListDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Check if a requirement list already exists for this term/year
                var existingList = await _context.RequirementLists
                    .FirstOrDefaultAsync(rl => rl.Term == dto.Term && rl.AcademicYear == dto.AcademicYear);

                if (existingList != null)
                    return Conflict($"A requirement list already exists for {dto.Term} {dto.AcademicYear}.");

                var requirementList = new RequirementList
                {
                    Term = dto.Term,
                    AcademicYear = dto.AcademicYear,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = (Guid)userId,
                    Status = "Active"
                };

                // Items are now added separately
                await _context.RequirementLists.AddAsync(requirementList);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRequirementListById), new { id = requirementList.Id },
                    new { requirementList.Id, requirementList.Term, requirementList.AcademicYear });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("requirement-lists/{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveRequirementList(Guid id)
        {
            try
            {
                var list = await _context.RequirementLists.FindAsync(id);
                if (list == null)
                    return NotFound("Requirement list not found.");

                list.Status = "Archived";
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Requirement list archived successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ----------- Requirement Items Methods -----------

        [HttpGet("requirement-items/{id}")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> GetRequirementItemById(Guid id)
        {
            try
            {
                var item = await _context.RequirementItems
                    .Include(ri => ri.RequirementList)
                    .FirstOrDefaultAsync(ri => ri.Id == id);

                if (item == null)
                    return NotFound("Requirement item not found.");

                var result = new RequirementItemDto
                {
                    Id = item.Id,
                    ItemName = item.ItemName,
                    RequiredQuantity = item.RequiredQuantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Description = item.Description
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("requirement-lists/{requirementListId}/items")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddItemToRequirementList(Guid requirementListId, [FromBody] CreateRequirementItemDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Find the requirement list
                var requirementList = await _context.RequirementLists
                    .Include(rl => rl.Items)
                    .FirstOrDefaultAsync(rl => rl.Id == requirementListId);

                if (requirementList == null)
                    return NotFound("Requirement list not found.");

                // Check if list is archived
                if (requirementList.Status == "Archived")
                    return BadRequest("Cannot add items to an archived requirement list.");

                // Check for duplicate item names in this list
                if (requirementList.Items.Any(i => i.ItemName == dto.ItemName))
                    return Conflict($"An item named '{dto.ItemName}' already exists in this requirement list.");

                // Create and add the new item
                var newItem = new RequirementItem
                {
                    RequirementListId = requirementListId,
                    ItemName = dto.ItemName,
                    RequiredQuantity = dto.RequiredQuantity,
                    Unit = dto.Unit,
                    UnitPrice = dto.UnitPrice,
                    Description = dto.Description
                };

                await _context.RequirementItems.AddAsync(newItem);
                await _context.SaveChangesAsync();

                // Update all student requirements that use this list
                await UpdateStudentRequirementsForNewItem(requirementListId);

                return CreatedAtAction(nameof(GetRequirementItemById),
                    new { id = newItem.Id },
                    new RequirementItemDto
                    {
                        Id = newItem.Id,
                        ItemName = newItem.ItemName,
                        RequiredQuantity = newItem.RequiredQuantity,
                        Unit = newItem.Unit,
                        UnitPrice = newItem.UnitPrice,
                        Description = newItem.Description
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("requirement-items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRequirementItem(Guid id, [FromBody] UpdateRequirementItemDto dto)
        {
            try
            {
                var item = await _context.RequirementItems
                    .Include(ri => ri.RequirementList)
                    .FirstOrDefaultAsync(ri => ri.Id == id);

                if (item == null)
                    return NotFound("Requirement item not found.");

                // Check if list is archived
                if (item.RequirementList.Status == "Archived")
                    return BadRequest("Cannot modify items in an archived requirement list.");

                // Update properties if provided
                if (!string.IsNullOrEmpty(dto.ItemName))
                {
                    // Check for duplicate name
                    var duplicateExists = await _context.RequirementItems
                        .AnyAsync(ri => ri.RequirementListId == item.RequirementListId &&
                                       ri.Id != id &&
                                       ri.ItemName == dto.ItemName);

                    if (duplicateExists)
                        return Conflict($"An item named '{dto.ItemName}' already exists in this requirement list.");

                    item.ItemName = dto.ItemName;
                }

                if (dto.RequiredQuantity.HasValue)
                    item.RequiredQuantity = dto.RequiredQuantity.Value;

                if (!string.IsNullOrEmpty(dto.Unit))
                    item.Unit = dto.Unit;

                if (dto.UnitPrice.HasValue)
                    item.UnitPrice = dto.UnitPrice.Value;

                if (dto.Description != null)
                    item.Description = dto.Description;

                await _context.SaveChangesAsync();

                // Update student requirements if quantity or price changed
                if (dto.RequiredQuantity.HasValue || dto.UnitPrice.HasValue)
                {
                    await UpdateStudentRequirementsForItemChange(item.RequirementListId);
                }

                return Ok(new RequirementItemDto
                {
                    Id = item.Id,
                    ItemName = item.ItemName,
                    RequiredQuantity = item.RequiredQuantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Description = item.Description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("requirement-items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRequirementItem(Guid id)
        {
            try
            {
                var item = await _context.RequirementItems
                    .Include(ri => ri.RequirementList)
                    .FirstOrDefaultAsync(ri => ri.Id == id);

                if (item == null)
                    return NotFound("Requirement item not found.");

                // Check if list is archived
                if (item.RequirementList.Status == "Archived")
                    return BadRequest("Cannot delete items from an archived requirement list.");

                // Check if any transactions use this item
                var hasTransactions = await _context.ItemTransactions
                    .AnyAsync(t => t.RequirementItemId == id);

                if (hasTransactions)
                    return BadRequest("Cannot delete item as it has associated transactions.");

                var requirementListId = item.RequirementListId;
                _context.RequirementItems.Remove(item);
                await _context.SaveChangesAsync();

                // Update student requirements
                await UpdateStudentRequirementsForItemChange(requirementListId);

                return Ok(new { Message = "Requirement item deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ----------- Student Requirements Methods -----------

        [HttpGet("student-requirements")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> GetAllStudentRequirements([FromQuery] Guid? studentId, [FromQuery] string? term, [FromQuery] int? academicYear)
        {
            try
            {
                var query = _context.StudentRequirements
                    .Include(sr => sr.Student)
                    .Include(sr => sr.RequirementList)
                    .AsQueryable();

                if (studentId.HasValue)
                    query = query.Where(sr => sr.StudentId == studentId.Value);

                if (!string.IsNullOrEmpty(term))
                    query = query.Where(sr => sr.RequirementList.Term == term);

                if (academicYear.HasValue)
                    query = query.Where(sr => sr.RequirementList.AcademicYear == academicYear.Value);

                var studentRequirements = await query.ToListAsync();

                var result = studentRequirements.Select(sr => new
                {
                    sr.Id,
                    sr.StudentId,
                    StudentName = sr.Student.Name,
                    sr.RequirementListId,
                    sr.RequirementList.Term,
                    sr.RequirementList.AcademicYear,
                    sr.Status,
                    sr.AssignedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("student-requirements/{id}")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> GetStudentRequirementById(Guid id)
        {
            try
            {
                var studentRequirement = await _context.StudentRequirements
                    .Include(sr => sr.Student)
                    .Include(sr => sr.RequirementList)
                    .Include(sr => sr.RequirementList.Items)
                    .Include(sr => sr.Transactions)
                    .FirstOrDefaultAsync(sr => sr.Id == id);

                if (studentRequirement == null)
                    return NotFound("Student requirement not found.");

                // Calculate status for each item
                var itemStatus = new List<RequirementStatusDto>();
                foreach (var item in studentRequirement.RequirementList.Items)
                {
                    // Calculate received quantity from transactions
                    var itemTransactions = studentRequirement.Transactions
                        .Where(t => t.RequirementItemId == item.Id && t.TransactionType == "Item")
                        .Sum(t => t.ItemQuantity ?? 0);

                    // Calculate money allocations for this specific item
                    var itemSpecificMoney = studentRequirement.Transactions
                        .Where(t => t.TransactionType == "Money" && t.RequirementItemId == item.Id)
                        .Sum(t => t.MoneyAmount ?? 0);

                    // Calculate general money allocations
                    var generalMoney = studentRequirement.Transactions
                        .Where(t => t.TransactionType == "Money" && t.RequirementItemId == null)
                        .Sum(t => t.MoneyAmount ?? 0);

                    // Convert money to quantity equivalent
                    decimal itemSpecificMoneyAllocation = itemSpecificMoney / item.UnitPrice;
                    
                    decimal generalMoneyAllocation = 0;
                    if (generalMoney > 0)
                    {
                        // Get total value of all items in this requirement
                        var totalValue = studentRequirement.RequirementList.Items.Sum(i => i.RequiredQuantity * i.UnitPrice);

                        // Calculate proportion of money that should go to this item
                        var itemProportion = (item.RequiredQuantity * item.UnitPrice) / totalValue;
                        generalMoneyAllocation = generalMoney * itemProportion / item.UnitPrice;
                    }

                    var totalReceived = itemTransactions + itemSpecificMoneyAllocation + generalMoneyAllocation;
                    var outstanding = Math.Max(0, item.RequiredQuantity - totalReceived);

                    itemStatus.Add(new RequirementStatusDto
                    {
                        ItemId = item.Id,
                        ItemName = item.ItemName,
                        Unit = item.Unit,
                        RequiredQuantity = item.RequiredQuantity,
                        ReceivedQuantity = totalReceived,
                        OutstandingQuantity = outstanding,
                        UnitPrice = item.UnitPrice,
                        MonetaryEquivalent = outstanding * item.UnitPrice,
                        IsFulfilled = outstanding <= 0
                    });
                }

                var result = new StudentRequirementDto
                {
                    Id = studentRequirement.Id,
                    StudentId = studentRequirement.StudentId,
                    StudentName = studentRequirement.Student.Name,
                    RequirementListId = studentRequirement.RequirementListId,
                    Term = studentRequirement.RequirementList.Term,
                    AcademicYear = studentRequirement.RequirementList.AcademicYear,
                    Status = studentRequirement.Status,
                    AssignedAt = studentRequirement.AssignedAt,
                    RequirementItems = itemStatus
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("student-requirements")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> AssignRequirementToStudent([FromBody] AssignRequirementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Validate student and requirement list
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return NotFound("Student not found.");

                var requirementList = await _context.RequirementLists.FindAsync(dto.RequirementListId);
                if (requirementList == null)
                    return NotFound("Requirement list not found.");

                // Check if student already has this requirement
                var existingRequirement = await _context.StudentRequirements
                    .FirstOrDefaultAsync(sr => sr.StudentId == dto.StudentId && sr.RequirementListId == dto.RequirementListId);

                if (existingRequirement != null)
                    return Conflict("This requirement has already been assigned to this student.");

                var studentRequirement = new StudentRequirement
                {
                    StudentId = dto.StudentId,
                    RequirementListId = dto.RequirementListId,
                    Status = "Pending",
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = (Guid)userId
                };

                await _context.StudentRequirements.AddAsync(studentRequirement);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetStudentRequirementById), new { id = studentRequirement.Id }, studentRequirement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("assign-students")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRequirementToMultipleStudents([FromBody] BulkAssignRequirementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Validate requirement list
                var requirementList = await _context.RequirementLists.FindAsync(dto.RequirementListId);
                if (requirementList == null)
                    return NotFound("Requirement list not found.");

                // Process each student
                var assignedCount = 0;
                var skippedCount = 0;

                foreach (var studentId in dto.StudentIds)
                {
                    // Validate student
                    var student = await _context.Students.FindAsync(studentId);
                    if (student == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Check if student already has this requirement
                    var existingRequirement = await _context.StudentRequirements
                        .AnyAsync(sr => sr.StudentId == studentId && sr.RequirementListId == dto.RequirementListId);

                    if (existingRequirement)
                    {
                        skippedCount++;
                        continue;
                    }

                    var studentRequirement = new StudentRequirement
                    {
                        StudentId = studentId,
                        RequirementListId = dto.RequirementListId,
                        Status = "Pending",
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = (Guid)userId
                    };

                    await _context.StudentRequirements.AddAsync(studentRequirement);
                    assignedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Requirements assigned to students",
                    RequirementList = new
                    {
                        requirementList.Id,
                        requirementList.Term,
                        requirementList.AcademicYear
                    },
                    StudentsAssigned = assignedCount,
                    StudentsSkipped = skippedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ----------- Transactions Methods -----------

        [HttpPost("transactions")]
        [Authorize(Roles = "Admin,Accountant,StockManager")]
        public async Task<IActionResult> RecordTransaction([FromBody] RecordTransactionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token.");

                // Validate student requirement
                var studentRequirement = await _context.StudentRequirements
                    .Include(sr => sr.Student)
                    .Include(sr => sr.RequirementList)
                    .Include(sr => sr.RequirementList.Items)
                    .FirstOrDefaultAsync(sr => sr.Id == dto.StudentRequirementId);

                if (studentRequirement == null)
                    return NotFound("Student requirement not found.");

                // Use a database transaction to ensure atomicity
                using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Process each transaction item
                    var transactionItems = new List<ItemTransaction>();
                    foreach (var item in dto.Items)
                    {
                        if (item.TransactionType == "Item")
                        {
                            if (!await ValidateItemTransaction(item, studentRequirement))
                                return BadRequest($"Invalid item transaction for item ID {item.RequirementItemId}");

                            var transaction = new ItemTransaction
                            {
                                StudentRequirementId = dto.StudentRequirementId,
                                TransactionDate = dto.TransactionDate,
                                TransactionType = "Item",
                                RequirementItemId = item.RequirementItemId,
                                ItemQuantity = item.ItemQuantity,
                                Notes = item.Notes,
                                RecordedBy = (Guid)userId
                            };

                            transactionItems.Add(transaction);
                        }
                        else if (item.TransactionType == "Money")
                        {
                            if (!item.MoneyAmount.HasValue || item.MoneyAmount <= 0)
                                return BadRequest("Money amount must be greater than zero.");

                            // Validate RequirementItemId for money transactions if provided
                            if (item.RequirementItemId.HasValue)
                            {
                                var requirementItem = await _context.RequirementItems
                                    .FirstOrDefaultAsync(ri => ri.Id == item.RequirementItemId &&
                                                               ri.RequirementListId == studentRequirement.RequirementListId);

                                if (requirementItem == null)
                                    return BadRequest($"Invalid requirement item ID {item.RequirementItemId} for money transaction.");
                            }

                            var transaction = new ItemTransaction
                            {
                                StudentRequirementId = dto.StudentRequirementId,
                                TransactionDate = dto.TransactionDate,
                                TransactionType = "Money",
                                RequirementItemId = item.RequirementItemId,
                                MoneyAmount = item.MoneyAmount,
                                Notes = item.Notes,
                                RecordedBy = (Guid)userId
                            };

                            transactionItems.Add(transaction);
                        }
                        else
                        {
                            return BadRequest($"Invalid transaction type: {item.TransactionType}. Must be 'Item' or 'Money'.");
                        }
                    }

                    // Save ItemTransactions first to get their IDs
                    await _context.ItemTransactions.AddRangeAsync(transactionItems);
                    await _context.SaveChangesAsync();

                    // Create FinancialTransactions for each ItemTransaction
                    foreach (var itemTransaction in transactionItems)
                    {
                        await CreateFinancialTransactionForItemAsync(itemTransaction, (Guid)userId);
                    }

                    // Update student requirement status
                    await UpdateStudentRequirementStatus(studentRequirement, transactionItems);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    // Prepare response
                    var response = new TransactionResponseDto
                    {
                        Id = transactionItems.First().Id,
                        StudentRequirementId = dto.StudentRequirementId,
                        StudentName = studentRequirement.Student.Name,
                        Term = studentRequirement.RequirementList.Term,
                        AcademicYear = studentRequirement.RequirementList.AcademicYear,
                        TransactionDate = dto.TransactionDate,
                        Items = [.. transactionItems.Select(t => new TransactionDetailDto
                        {
                            Id = t.Id,
                            TransactionType = t.TransactionType,
                            ItemName = t.RequirementItem.ItemName,
                            Quantity = t.ItemQuantity,
                            Unit = t.RequirementItem.Unit,
                            MoneyAmount = t.MoneyAmount,
                            Notes = t.Notes
                        })],
                        RequirementFulfilled = studentRequirement.Status == "Complete"
                    };

                    return Ok(response);
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ----------- Helper Methods -----------

        private async Task<bool> ValidateItemTransaction(TransactionItemDto item, StudentRequirement studentRequirement)
        {
            // Validate the requirement item
            if (!item.RequirementItemId.HasValue)
                return false;

            var requirementItem = await _context.RequirementItems
                .FirstOrDefaultAsync(ri => ri.Id == item.RequirementItemId &&
                                           ri.RequirementListId == studentRequirement.RequirementListId);

            if (requirementItem == null)
                return false;

            if (!item.ItemQuantity.HasValue || item.ItemQuantity <= 0)
                return false;

            return true;
        }

        private async Task UpdateStudentRequirementStatus(StudentRequirement studentRequirement, List<ItemTransaction> newTransactions)
        {
            // Get all transactions for this requirement (existing + new ones)
            var existingTransactions = await _context.ItemTransactions
                .Where(t => t.StudentRequirementId == studentRequirement.Id)
                .ToListAsync();

            var allTransactions = existingTransactions.Concat(newTransactions).ToList();

            // Calculate fulfillment for each requirement item
            var allFulfilled = true;
            var anyFulfilled = false;

            foreach (var item in studentRequirement.RequirementList.Items)
            {
                // Calculate received quantity from item transactions
                var itemTransactions = allTransactions
                    .Where(t => t.RequirementItemId == item.Id && t.TransactionType == "Item")
                    .Sum(t => t.ItemQuantity ?? 0);

                // Calculate money contributions specifically for this item
                var itemSpecificMoney = allTransactions
                    .Where(t => t.TransactionType == "Money" && t.RequirementItemId == item.Id)
                    .Sum(t => t.MoneyAmount ?? 0);

                // Calculate general money contributions (not linked to specific items)
                var generalMoney = allTransactions
                    .Where(t => t.TransactionType == "Money" && t.RequirementItemId == null)
                    .Sum(t => t.MoneyAmount ?? 0);

                // Convert item-specific money to quantity equivalent
                decimal itemSpecificMoneyAllocation = itemSpecificMoney / item.UnitPrice;

                // Distribute general money across items
                decimal generalMoneyAllocation = 0;
                if (generalMoney > 0)
                {
                    // Get total value of all items in this requirement
                    var totalValue = studentRequirement.RequirementList.Items.Sum(i => i.RequiredQuantity * i.UnitPrice);

                    // Calculate proportion of money that should go to this item
                    var itemProportion = (item.RequiredQuantity * item.UnitPrice) / totalValue;
                    generalMoneyAllocation = generalMoney * itemProportion / item.UnitPrice;
                }

                var totalReceived = itemTransactions + itemSpecificMoneyAllocation + generalMoneyAllocation;
                var isFulfilled = totalReceived >= item.RequiredQuantity;

                if (!isFulfilled)
                    allFulfilled = false;
                else
                    anyFulfilled = true;
            }

            // Update the requirement status
            if (allFulfilled)
                studentRequirement.Status = "Complete";
            else if (anyFulfilled)
                studentRequirement.Status = "Partial";
            else
                studentRequirement.Status = "Pending";
        }

        private async Task UpdateStudentRequirementsForNewItem(Guid requirementListId)
        {
            // Find all student requirements for this list
            var studentRequirements = await _context.StudentRequirements
                .Where(sr => sr.RequirementListId == requirementListId)
                .ToListAsync();

            // For each student requirement, update the status
            // Adding a new required item may change a "Complete" status to "Partial"
            foreach (var requirement in studentRequirements)
            {
                if (requirement.Status == "Complete")
                {
                    requirement.Status = "Partial";  // A new item was added, so it's now partial
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateStudentRequirementsForItemChange(Guid requirementListId)
        {
            // Find all student requirements for this list
            var studentRequirements = await _context.StudentRequirements
                .Where(sr => sr.RequirementListId == requirementListId)
                .Include(sr => sr.RequirementList)
                .Include(sr => sr.RequirementList.Items)
                .Include(sr => sr.Transactions)
                .ToListAsync();

            // Recalculate status for each requirement
            foreach (var requirement in studentRequirements)
            {
                await UpdateStudentRequirementStatus(requirement, []);
            }
        }

        private async Task CreateFinancialTransactionForItemAsync(ItemTransaction itemTransaction, Guid userId)
        {
            // Load related data if not already loaded
            if (itemTransaction.RequirementItem == null && itemTransaction.RequirementItemId.HasValue)
            {
                var requirementItem = await _context.RequirementItems
                    .FirstOrDefaultAsync(ri => ri.Id == itemTransaction.RequirementItemId.Value);
                if (requirementItem != null)
                {
                    itemTransaction.RequirementItem = requirementItem;
                }
            }

            if (itemTransaction.StudentRequirement == null)
            {
                var sr = await _context.StudentRequirements
                    .Include(sr => sr.Student)
                    .Include(sr => sr.RequirementList)
                    .FirstOrDefaultAsync(sr => sr.Id == itemTransaction.StudentRequirementId);

                if (sr != null)
                {
                    itemTransaction.StudentRequirement = sr;
                }
            }

            decimal amount = 0;
            string description = "";
            string category = "";

            if (itemTransaction.TransactionType == "Item")
            {
                var unitPrice = itemTransaction.RequirementItem?.UnitPrice ?? 0;
                var quantity = itemTransaction.ItemQuantity ?? 0;
                amount = unitPrice * quantity;

                var itemName = itemTransaction.RequirementItem?.ItemName ?? "Item";
                var unit = itemTransaction.RequirementItem?.Unit ?? "units";
                description = $"Item received: {itemName} ({quantity} {unit}) - {itemTransaction.StudentRequirement?.Student?.Name}";
                category = "Item Receipt";
            }
            else if (itemTransaction.TransactionType == "Money")
            {
                amount = itemTransaction.MoneyAmount ?? 0;

                if (itemTransaction.RequirementItemId.HasValue)
                {
                    var itemName = itemTransaction.RequirementItem?.ItemName ?? "Item";
                    description = $"Money contribution for: {itemName} - {itemTransaction.StudentRequirement?.Student?.Name}";
                }
                else
                {
                    description = $"Money contribution for requirement items - {itemTransaction.StudentRequirement?.Student?.Name}";
                }
                category = "Money Contribution";
            }
            else
            {
                amount = itemTransaction.MoneyAmount ?? 0;
                description = $"{itemTransaction.TransactionType}: {itemTransaction.Notes ?? "Transaction"}";
                category = itemTransaction.TransactionType;
            }

            var financialTransaction = new FinancialTransaction
            {
                Date = itemTransaction.TransactionDate,
                Amount = amount,
                Type = "Item Transaction",
                Category = category,
                Description = description,
                CreatedBy = userId,
                ItemTransactionId = itemTransaction.Id,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            await _context.FinancialTransactions.AddAsync(financialTransaction);
        }
    }

    // New DTO for bulk assignment
    public class BulkAssignRequirementDto
    {
        public Guid RequirementListId { get; set; }
        public List<Guid> StudentIds { get; set; }
    }

    // New DTO for updating requirement items
    public class UpdateRequirementItemDto
    {
        public string ItemName { get; set; }
        public decimal? RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public string Description { get; set; }
    }
}
