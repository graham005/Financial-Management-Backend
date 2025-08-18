using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Financial_management_backend.Services.Dtos;
using Financial_management_backend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers.Accountant
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFinancialTransactionService _transactionService;

        public ExpenseController(ApplicationDbContext context, IFinancialTransactionService transactionService)
        {
            _context = context;
            _transactionService = transactionService;
        }

        // ----------- Expense Category Methods -----------

        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.ExpenseCategories.ToListAsync();
            return Ok(categories.Select(c => new ExpenseCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                BudgetAmount = c.BudgetAmount
            }));
        }

        [HttpGet("categories/{id}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await _context.ExpenseCategories.FindAsync(id);
            if (category == null)
                return NotFound("Category not found.");

            return Ok(new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                BudgetAmount = category.BudgetAmount
            });
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryDto dto)
        {
            var category = new ExpenseCategory
            {
                Name = dto.Name,
                Description = dto.Description,
                BudgetAmount = dto.BudgetAmount
            };
            await _context.ExpenseCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }

        [HttpPatch("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateExpenseCategoryDto dto)
        {
            var category = await _context.ExpenseCategories.FindAsync(id);
            if (category == null)
                return NotFound("Category not found.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                category.Name = dto.Name;
            if (dto.Description != null)
                category.Description = dto.Description;
            if (dto.BudgetAmount.HasValue)
                category.BudgetAmount = dto.BudgetAmount.Value;

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.ExpenseCategories.FindAsync(id);
            if (category == null)
                return NotFound("Category not found.");

            _context.ExpenseCategories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok("Category deleted.");
        }

        // ----------- Expense Methods -----------

        [HttpGet]
        public async Task<IActionResult> GetAllExpenses()
        {
            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .ToListAsync();

            return Ok(expenses.Select(e => new ExpenseDto
            {
                Id = e.Id,
                Date = e.Date,
                Amount = e.Amount,
                ExpenseCategoryId = e.ExpenseCategoryId,
                CategoryName = e.Category.Name,
                Vendor = e.Vendor,
                Description = e.Description,
                ApprovalStatus = e.ApprovalStatus,
                CreatedById = e.CreatedById,
                CreatedAt = e.CreatedAt
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseById(Guid id)
        {
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null)
                return NotFound("Expense not found.");

            return Ok(new ExpenseDto
            {
                Id = expense.Id,
                Date = expense.Date,
                Amount = expense.Amount,
                ExpenseCategoryId = expense.ExpenseCategoryId,
                CategoryName = expense.Category.Name,
                Vendor = expense.Vendor,
                Description = expense.Description,
                ApprovalStatus = expense.ApprovalStatus,
                CreatedById = expense.CreatedById,
                CreatedAt = expense.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
        {
            var category = await _context.ExpenseCategories.FindAsync(dto.ExpenseCategoryId);
            if (category == null)
                return NotFound("Expense category not found.");

            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var expense = new Expense
            {
                Date = dto.Date,
                Amount = dto.Amount,
                ExpenseCategoryId = dto.ExpenseCategoryId,
                Vendor = dto.Vendor,
                Description = dto.Description,
                ApprovalStatus = "Pending",
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();

            // Log the financial transaction
            var transaction = new FinancialTransaction
            {
                Date = expense.Date,
                Amount = expense.Amount,
                Type = "Expense",
                Category = category.Name,
                Description = expense.Description,
                CreatedBy = userId.Value,
                ExpenseId = expense.Id,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };
            await _transactionService.CreateAsync(transaction);

            return CreatedAtAction(nameof(GetExpenseById), new { id = expense.Id }, expense);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseDto dto)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound("Expense not found.");

            if (dto.Date.HasValue)
                expense.Date = dto.Date.Value;
            if (dto.Amount.HasValue)
                expense.Amount = dto.Amount.Value;
            if (dto.ExpenseCategoryId.HasValue)
            {
                var category = await _context.ExpenseCategories.FindAsync(dto.ExpenseCategoryId.Value);
                if (category == null)
                    return NotFound("Expense category not found.");
                expense.ExpenseCategoryId = dto.ExpenseCategoryId.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.Vendor))
                expense.Vendor = dto.Vendor;
            if (dto.Description != null)
                expense.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.ApprovalStatus))
                expense.ApprovalStatus = dto.ApprovalStatus;

            expense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(expense);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(Guid id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound("Expense not found.");

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return Ok("Expense deleted.");
        }
    }
}
