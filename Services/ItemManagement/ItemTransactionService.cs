using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Models.ItemManagement;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services.ItemManagement
{
    public interface IItemTransactionService
    {
        Task<ItemTransaction> CreateAsync(ItemTransaction transaction, Guid userId);
        Task<List<ItemTransaction>> CreateBatchAsync(List<ItemTransaction> transactions, Guid userId);
        Task<ItemTransaction> GetByIdAsync(Guid id);
        Task<List<ItemTransaction>> GetByStudentRequirementAsync(Guid studentRequirementId);
        Task<List<ItemTransaction>> GetByStudentAsync(Guid studentId);
        Task UpdateStatusOfRequirementAsync(Guid studentRequirementId);
        Task DeleteAsync(Guid id);
    }

    public class ItemTransactionService(ApplicationDbContext context) : IItemTransactionService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ItemTransaction> CreateAsync(ItemTransaction transaction, Guid userId)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ItemTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                // Create corresponding FinancialTransaction
                await CreateFinancialTransactionAsync(transaction, userId);

                // Update the status of the student requirement
                await UpdateStatusOfRequirementAsync(transaction.StudentRequirementId);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ItemTransaction>> CreateBatchAsync(List<ItemTransaction> transactions, Guid userId)
        {
            if (transactions == null || !transactions.Any())
                return new List<ItemTransaction>();

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ItemTransactions.AddRangeAsync(transactions);
                await _context.SaveChangesAsync();

                // Create corresponding FinancialTransactions for each item transaction
                foreach (var transaction in transactions)
                {
                    await CreateFinancialTransactionAsync(transaction, userId);
                }

                // Update the status of the student requirement
                var studentRequirementId = transactions.First().StudentRequirementId;
                await UpdateStatusOfRequirementAsync(studentRequirementId);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return transactions;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        private async Task CreateFinancialTransactionAsync(ItemTransaction itemTransaction, Guid userId)
        {
            // Load related data if needed
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
                var studentRequirement = await _context.StudentRequirements
                    .Include(sr => sr.Student)
                    .Include(sr => sr.RequirementList)
                    .FirstOrDefaultAsync(sr => sr.Id == itemTransaction.StudentRequirementId);

                if (studentRequirement != null)
                {
                    itemTransaction.StudentRequirement = studentRequirement;
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
                var studentName = itemTransaction.StudentRequirement?.Student?.Name ?? "Student";

                description = $"Item received: {itemName} ({quantity} {unit}) - {studentName}";
                category = "Item Receipt";
            }
            else if (itemTransaction.TransactionType == "Money")
            {
                amount = itemTransaction.MoneyAmount ?? 0;
                var studentName = itemTransaction.StudentRequirement?.Student?.Name ?? "Student";

                if (itemTransaction.RequirementItemId.HasValue)
                {
                    description = $"Money contribution for: {itemTransaction.RequirementItem?.ItemName ?? "Item"} - {studentName}";
                }
                else
                {
                    description = $"Money contribution for requirement items - {studentName}";
                }
                category = "Money Contribution";
            }
            else
            {
                // Adjustment or other types
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

        public async Task<ItemTransaction> GetByIdAsync(Guid id)
        {
            var transaction = await _context.ItemTransactions
                .Include(t => t.StudentRequirement)
                    .ThenInclude(sr => sr.Student)
                .Include(t => t.StudentRequirement)
                    .ThenInclude(sr => sr.RequirementList)
                .Include(t => t.RequirementItem)
                .Include(t => t.Recorder)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                throw new InvalidOperationException($"ItemTransaction with id '{id}' was not found.");

            return transaction;
        }

        public async Task<List<ItemTransaction>> GetByStudentRequirementAsync(Guid studentRequirementId)
        {
            return await _context.ItemTransactions
                .Include(t => t.RequirementItem)
                .Include(t => t.Recorder)
                .Where(t => t.StudentRequirementId == studentRequirementId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<ItemTransaction>> GetByStudentAsync(Guid studentId)
        {
            return await _context.ItemTransactions
                .Include(t => t.StudentRequirement)
                    .ThenInclude(sr => sr.RequirementList)
                .Include(t => t.RequirementItem)
                .Where(t => t.StudentRequirement.StudentId == studentId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task UpdateStatusOfRequirementAsync(Guid studentRequirementId)
        {
            var studentRequirement = await _context.StudentRequirements
                .Include(sr => sr.RequirementList)
                    .ThenInclude(rl => rl.Items)
                .Include(sr => sr.Transactions)
                .FirstOrDefaultAsync(sr => sr.Id == studentRequirementId);

            if (studentRequirement == null)
                throw new InvalidOperationException($"StudentRequirement with id '{studentRequirementId}' was not found.");

            // Calculate fulfillment for each requirement item
            var allFulfilled = true;
            var anyFulfilled = false;

            foreach (var item in studentRequirement.RequirementList.Items)
            {
                // Calculate received quantity from item transactions
                var itemTransactions = studentRequirement.Transactions
                    .Where(t => t.RequirementItemId == item.Id && t.TransactionType == "Item")
                    .Sum(t => t.ItemQuantity ?? 0);

                // Calculate total money contributions
                var moneyTotal = studentRequirement.Transactions
                    .Where(t => t.TransactionType == "Money")
                    .Sum(t => t.MoneyAmount ?? 0);

                // Distribute money across items
                decimal moneyAllocation = 0;
                if (moneyTotal > 0)
                {
                    // Get total value of all items in this requirement
                    var totalValue = studentRequirement.RequirementList.Items.Sum(i => i.RequiredQuantity * i.UnitPrice);

                    // Calculate proportion of money that should go to this item
                    var itemProportion = (item.RequiredQuantity * item.UnitPrice) / totalValue;
                    moneyAllocation = moneyTotal * itemProportion / item.UnitPrice;
                }

                var totalReceived = itemTransactions + moneyAllocation;
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

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transaction = await _context.ItemTransactions.FindAsync(id) ?? throw new InvalidOperationException($"ItemTransaction with id '{id}' was not found.");
                var studentRequirementId = transaction.StudentRequirementId;

                // Delete associated FinancialTransaction if exists
                var financialTransaction = await _context.FinancialTransactions
                    .FirstOrDefaultAsync(ft => ft.ItemTransactionId == id);

                if (financialTransaction != null)
                {
                    _context.FinancialTransactions.Remove(financialTransaction);
                }

                _context.ItemTransactions.Remove(transaction);
                await _context.SaveChangesAsync();

                // Update the status of the student requirement
                await UpdateStatusOfRequirementAsync(studentRequirementId);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}