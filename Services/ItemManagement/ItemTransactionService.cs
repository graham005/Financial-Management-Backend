using Financial_management_backend.Data;
using Financial_management_backend.Models.ItemManagement;
using Financial_management_backend.Services.Dtos.ItemManagement;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services.ItemManagement
{
    public interface IItemTransactionService
    {
        Task<ItemTransaction> CreateAsync(ItemTransaction transaction);
        Task<List<ItemTransaction>> CreateBatchAsync(List<ItemTransaction> transactions);
        Task<ItemTransaction> GetByIdAsync(Guid id);
        Task<List<ItemTransaction>> GetByStudentRequirementAsync(Guid studentRequirementId);
        Task<List<ItemTransaction>> GetByStudentAsync(Guid studentId);
        Task UpdateStatusOfRequirementAsync(Guid studentRequirementId);
        Task DeleteAsync(Guid id);
    }

    public class ItemTransactionService : IItemTransactionService
    {
        private readonly ApplicationDbContext _context;

        public ItemTransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ItemTransaction> CreateAsync(ItemTransaction transaction)
        {
            await _context.ItemTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            
            // Update the status of the student requirement
            await UpdateStatusOfRequirementAsync(transaction.StudentRequirementId);
            
            return transaction;
        }

        public async Task<List<ItemTransaction>> CreateBatchAsync(List<ItemTransaction> transactions)
        {
            if (transactions == null || !transactions.Any())
                return new List<ItemTransaction>();

            await _context.ItemTransactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();

            // Update the status of the student requirement
            var studentRequirementId = transactions.First().StudentRequirementId;
            await UpdateStatusOfRequirementAsync(studentRequirementId);

            return transactions;
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
            var transaction = await _context.ItemTransactions.FindAsync(id);
            if (transaction == null)
                throw new InvalidOperationException($"ItemTransaction with id '{id}' was not found.");

            var studentRequirementId = transaction.StudentRequirementId;
            
            _context.ItemTransactions.Remove(transaction);
            await _context.SaveChangesAsync();
            
            // Update the status of the student requirement
            await UpdateStatusOfRequirementAsync(studentRequirementId);
        }
    }
}