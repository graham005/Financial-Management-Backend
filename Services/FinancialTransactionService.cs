using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Services
{
    public interface IFinancialTransactionService
    {
        Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction);
        Task<FinancialTransaction> GetByIdAsync(Guid id);
        Task<IEnumerable<FinancialTransaction>> GetAllAsync();
        Task<IEnumerable<FinancialTransaction>> GetByTypeAsync(string type);
        Task DeleteAsync(Guid id);
    }

    public class FinancialTransactionService : IFinancialTransactionService
    {
        private readonly ApplicationDbContext _context;

        public FinancialTransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction)
        {
            bool hasPayment = transaction.PaymentId.HasValue;
            bool hasExpense = transaction.ExpenseId.HasValue;

            if(hasPayment == hasExpense)
            {
                throw new ArgumentException("A transaction must have either a Payment or Expense, but not both or neither");
            }
            await _context.FinancialTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<FinancialTransaction> GetByIdAsync(Guid id)
        {
            var transaction = await _context.FinancialTransactions
                .Include(t => t.Payment)
                .Include(t => t.Expense)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                throw new InvalidOperationException($"FinancialTransaction with id '{id}' was not found.");
            }

            return transaction;
        }

        public async Task<IEnumerable<FinancialTransaction>> GetAllAsync()
        {
            return await _context.FinancialTransactions
                .Include(t => t.Payment)
                .Include(t => t.Expense)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<FinancialTransaction>> GetByTypeAsync(string type)
        {
            return await _context.FinancialTransactions
                .Where(t => t.Type == type)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var transaction = await _context.FinancialTransactions.FindAsync(id);
            if (transaction != null)
            {
                _context.FinancialTransactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
        }
    }
}

