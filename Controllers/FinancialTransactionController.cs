using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financial_management_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FinancialTransactionController : ControllerBase
    {
        private readonly IFinancialTransactionService _transactionService;

        public FinancialTransactionController(IFinancialTransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var transactions = await _transactionService.GetAllAsync();
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var transaction = await _transactionService.GetByIdAsync(id);
            if (transaction == null)
                return NotFound("Transaction not found.");
            return Ok(transaction);
        }

        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetByType(string type)
        {
            var transactions = await _transactionService.GetByTypeAsync(type);
            return Ok(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FinancialTransaction transaction)
        {
            bool hasPayment = transaction.PaymentId.HasValue;
            bool hasExpense = transaction.ExpenseId.HasValue;

            if (hasPayment == hasExpense)
                return BadRequest("A transaction must have either a Payment or Expense, but not both or neither");
            var created = await _transactionService.CreateAsync(transaction);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _transactionService.DeleteAsync(id);
            return Ok("Transaction deleted.");
        }

        [HttpGet("{id}/receipt-data")]
        public async Task<IActionResult> GetReceiptData(Guid id)
        {
            try
            {
                var receiptData = await _transactionService.GetReceiptDataAsync(id);
                if (receiptData == null)
                    return NotFound("Transaction not found or receipt data unavailable.");

                return Ok(receiptData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while generating receipt data.");
            }
        }

        [HttpGet("{id}/thermal-receipt")]
        public async Task<IActionResult> GetThermalReceiptData(Guid id)
        {
            try
            {
                var thermalReceipt = await _transactionService.GetThermalReceiptDataAsync(id);
                if (thermalReceipt == null)
                    return NotFound("Transaction not found or receipt data unavailable.");

                return Ok(thermalReceipt);
            }
            catch (Exception )
            {
                return StatusCode(500, "An error occurred while generating thermal receipt data.");
            }
        }
    }
}