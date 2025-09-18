using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;
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
        Task<ReceiptDataDto> GetReceiptDataAsync(Guid transactionId);
        Task<ThermalReceiptDto> GetThermalReceiptDataAsync(Guid transactionId);
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

        public async Task<ReceiptDataDto> GetReceiptDataAsync(Guid transactionId)
        {
            var transaction = await _context.FinancialTransactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(s => s.Grade)
                .Include(t => t.Payment)
                    .ThenInclude(p => p.FeePayments)
                .Include(t => t.Expense)
                    .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return null;

            // Generate receipt number
            var receiptNumber = GenerateReceiptNumber(transaction);

            var receiptData = new ReceiptDataDto
            {
                ReceiptNumber = receiptNumber,
                ReceiptType = transaction.PaymentId.HasValue ? "Payment" : "Item",
                IssuedDate = DateTime.UtcNow,
                Organization = GetOrganizationDetails(),
                Transaction = new TransactionDetailsDto
                {
                    TransactionId = transaction.Id,
                    TotalAmount = transaction.Amount,
                    Description = transaction.Description,
                    TransactionDate = transaction.Date,
                    Status = transaction.Status
                },
                Notes = $"Receipt generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            };

            if (transaction.PaymentId.HasValue && transaction.Payment != null)
            {
                // Payment receipt
                await PopulatePaymentReceiptData(receiptData, transaction);
            }
            else if (transaction.ExpenseId.HasValue && transaction.Expense != null)
            {
                // Item/Expense receipt
                await PopulateItemReceiptData(receiptData, transaction);
            }

            return receiptData;
        }

        private string GenerateReceiptNumber(FinancialTransaction transaction)
        {
            var prefix = transaction.PaymentId.HasValue ? "RCP-PAY" : "RCP-ITM";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var shortId = transaction.Id.ToString()[..8];
            return $"{prefix}-{timestamp}-{shortId.ToUpper()}";
        }

        private OrganizationDetailsDto GetOrganizationDetails()
        {
            // You can configure these values or pull from database/configuration
            return new OrganizationDetailsDto
            {
                Name = "Your School Name",
                Address = "123 School Street, City, Country",
                Phone = "+1-234-567-8900",
                Email = "admin@yourschool.edu"
            };
        }

        private async Task PopulatePaymentReceiptData(ReceiptDataDto receiptData, FinancialTransaction transaction)
        {
            var payment = transaction.Payment;
            var student = payment.Student;
            
            receiptData.Student = new StudentDetailsDto
            {
                StudentId = student.Id,
                Name = student.Name,
                AdmissionNumber = student.AdmissionNumber,
                Grade = student.Grade?.Name ?? "N/A"
            };

            receiptData.Transaction.PaymentMethod = payment.PaymentMethod;
            
            // Get staff details
            var staff = await _context.Users.FindAsync(transaction.CreatedBy);
            receiptData.IssuedBy = new StaffDetailsDto
            {
                Name = staff?.Username ?? "System",
                Position = staff?.Role ?? "Staff",
                Department = "Finance"
            };

            // Build receipt items from fee payments
            var receiptItems = new List<ReceiptItemDto>();

            if (payment.FeePayments?.Any() == true)
            {
                foreach (var fp in payment.FeePayments)
                {
                    string feeName = await GetFeeNameByTypeAndId(fp.FeeType, fp.FeeId);
                    
                    receiptItems.Add(new ReceiptItemDto
                    {
                        ItemName = feeName,
                        Quantity = 1,
                        Unit = "Payment",
                        UnitPrice = fp.Amount,
                        TotalValue = fp.Amount,
                        ItemType = fp.FeeType,
                        Description = $"Payment for {feeName} - {fp.Term} {fp.Year}"
                    });
                }
            }
            else
            {
                // No specific fee payments, create general payment item
                receiptItems.Add(new ReceiptItemDto
                {
                    ItemName = "General Payment",
                    Quantity = 1,
                    Unit = "Payment",
                    UnitPrice = payment.Amount,
                    TotalValue = payment.Amount,
                    ItemType = "Fee",
                    Description = transaction.Description ?? "Payment"
                });
            }

            receiptData.Items = receiptItems;
        }

        private async Task<string> GetFeeNameByTypeAndId(string feeType, Guid feeId)
        {
            return feeType switch
            {
                "Tuition" => await GetTuitionFeeName(feeId),
                "Custom Tuition" => await GetCustomFeeName(feeId),
                _ => await GetOtherFeeName(feeId) ?? feeType
            };
        }

        private async Task<string> GetTuitionFeeName(Guid feeId)
        {
            var feeStructure = await _context.FeeStructures
                .Include(fs => fs.Grade)
                .FirstOrDefaultAsync(fs => fs.Id == feeId);
            
            return feeStructure != null ? $"Tuition Fee - {feeStructure.Grade.Name}" : "Tuition Fee";
        }

        private async Task<string> GetCustomFeeName(Guid feeId)
        {
            var customFee = await _context.CustomFees
                .Include(cf => cf.Student)
                .FirstOrDefaultAsync(cf => cf.Id == feeId);
            
            return customFee != null ? $"Custom Tuition - {customFee.Student.Name}" : "Custom Tuition";
        }

        private async Task<string?> GetOtherFeeName(Guid feeId)
        {
            var otherFee = await _context.OtherFees
                .FirstOrDefaultAsync(of => of.Id == feeId);
            
            return otherFee?.Name;
        }

        private async Task PopulateItemReceiptData(ReceiptDataDto receiptData, FinancialTransaction transaction)
        {
            var expense = transaction.Expense;
            
            // For expenses, you might want to show vendor as "student" equivalent
            receiptData.Student = new StudentDetailsDto
            {
                StudentId = Guid.Empty,
                Name = expense.Vendor ?? "N/A",
                AdmissionNumber = "N/A",
                Grade = "N/A"
            };

            // Get staff details
            var staff = await _context.Users.FindAsync(transaction.CreatedBy);
            receiptData.IssuedBy = new StaffDetailsDto
            {
                Name = staff?.Username ?? "System",
                Position = staff?.Role ?? "Staff",
                Department = "Finance"
            };

            receiptData.Items = new List<ReceiptItemDto>
            {
                new ReceiptItemDto
                {
                    ItemName = expense.Category?.Name ?? "Expense",
                    Quantity = 1,
                    Unit = "Item",
                    UnitPrice = expense.Amount,
                    TotalValue = expense.Amount,
                    ItemType = "Item",
                    Description = expense.Description ?? "Expense item"
                }
            };
        }

        public async Task<ThermalReceiptDto> GetThermalReceiptDataAsync(Guid transactionId)
        {
            var transaction = await _context.FinancialTransactions
                .Include(t => t.Payment)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(s => s.Grade)
                .Include(t => t.Payment)
                    .ThenInclude(p => p.FeePayments)
                .Include(t => t.Expense)
                    .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return null;

            var receiptNumber = GenerateReceiptNumber(transaction);
            var isPayment = transaction.PaymentId.HasValue;

            var thermalReceipt = new ThermalReceiptDto
            {
                Header = new ThermalReceiptHeader
                {
                    OrganizationName = TruncateText("Your School Name", 32),
                    OrganizationAddress = TruncateText("123 School Street, City", 32),
                    OrganizationPhone = "Tel: +1-234-567-8900",
                    OrganizationEmail = "admin@yourschool.edu",
                    ReceiptTitle = isPayment ? "PAYMENT RECEIPT" : "ITEM RECEIPT",
                    ReceiptNumber = receiptNumber,
                    IssuedDate = DateTime.UtcNow
                },
                PrinterSettings = GetThermalPrinterSettings()
            };

            if (isPayment)
            {
                await PopulateThermalPaymentReceipt(thermalReceipt, transaction);
            }
            else
            {
                await PopulateThermalItemReceipt(thermalReceipt, transaction);
            }

            return thermalReceipt;
        }

        private async Task PopulateThermalPaymentReceipt(ThermalReceiptDto receipt, FinancialTransaction transaction)
        {
            var payment = transaction.Payment;
            var student = payment.Student;

            // Customer Info
            receipt.Body = new ThermalReceiptBody
            {
                Customer = new ThermalCustomerInfo
                {
                    Name = TruncateText(student.Name, 25),
                    StudentNumber = student.AdmissionNumber,
                    Grade = student.Grade?.Name ?? "N/A",
                    ParentName = TruncateText("Parent Name", 25), // Get from parent relationship
                    Contact = "Contact Info" // Get from parent relationship
                }
            };

            // Items
            var thermalItems = new List<ThermalReceiptItem>();
            
            if (payment.FeePayments?.Any() == true)
            {
                foreach (var fp in payment.FeePayments)
                {
                    string feeName = await GetFeeNameByTypeAndId(fp.FeeType, fp.FeeId);
                    var item = new ThermalReceiptItem
                    {
                        Name = feeName,
                        ShortName = TruncateText(feeName, 20),
                        Quantity = 1,
                        Unit = "Fee",
                        UnitPrice = fp.Amount,
                        Total = fp.Amount
                    };
                    
                    // Pre-format line for thermal printing
                    item.FormattedLine = FormatReceiptLine(item.ShortName, item.Total);
                    thermalItems.Add(item);
                }
            }
            else
            {
                var item = new ThermalReceiptItem
                {
                    Name = "General Payment",
                    ShortName = "General Payment",
                    Quantity = 1,
                    Unit = "Payment",
                    UnitPrice = payment.Amount,
                    Total = payment.Amount
                };
                item.FormattedLine = FormatReceiptLine(item.ShortName, item.Total);
                thermalItems.Add(item);
            }

            receipt.Body.Items = thermalItems;

            // Totals
            receipt.Body.Totals = new ThermalReceiptTotals
            {
                SubTotal = payment.Amount,
                Tax = 0,
                Discount = 0,
                GrandTotal = payment.Amount,
                AmountInWords = ConvertAmountToWords(payment.Amount)
            };

            // Transaction Info
            receipt.Body.Transaction = new ThermalTransactionInfo
            {
                PaymentMethod = payment.PaymentMethod ?? "Cash",
                TransactionDate = transaction.Date,
                ProcessedBy = await GetStaffNameAsync(transaction.CreatedBy),
                Term = GetCurrentTerm(),
                Year = DateTime.Now.Year
            };

            // Footer
            receipt.Footer = new ThermalReceiptFooter
            {
                ThankYouMessage = "Thank you for your payment!",
                ContactInfo = "For inquiries: +1-234-567-8900",
                AdditionalNotes = new List<string>
                {
                    "Keep this receipt for your records",
                    "Valid for current academic year"
                },
                Signature = "Authorized Signature: _______________"
            };
        }

        private async Task PopulateThermalItemReceipt(ThermalReceiptDto receipt, FinancialTransaction transaction)
        {
            var expense = transaction.Expense;

            receipt.Body = new ThermalReceiptBody
            {
                Customer = new ThermalCustomerInfo
                {
                    Name = TruncateText(expense.Vendor ?? "N/A", 25),
                    StudentNumber = "N/A",
                    Grade = "N/A"
                }
            };

            var item = new ThermalReceiptItem
            {
                Name = expense.Category?.Name ?? "Item",
                ShortName = TruncateText(expense.Category?.Name ?? "Item", 20),
                Quantity = 1,
                Unit = "Item",
                UnitPrice = expense.Amount,
                Total = expense.Amount
            };
            item.FormattedLine = FormatReceiptLine(item.ShortName, item.Total);

            receipt.Body.Items = new List<ThermalReceiptItem> { item };
            receipt.Body.Totals = new ThermalReceiptTotals
            {
                GrandTotal = expense.Amount,
                AmountInWords = ConvertAmountToWords(expense.Amount)
            };

            receipt.Body.Transaction = new ThermalTransactionInfo
            {
                TransactionDate = transaction.Date,
                ProcessedBy = await GetStaffNameAsync(transaction.CreatedBy)
            };

            receipt.Footer = new ThermalReceiptFooter
            {
                ThankYouMessage = "Item receipt issued",
                AdditionalNotes = new List<string> { "Keep for records" }
            };
        }

        // Helper methods for thermal formatting
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? "";
                
            return text.Substring(0, maxLength - 3) + "...";
        }

        private string FormatReceiptLine(string itemName, decimal amount, int lineWidth = 32)
        {
            string amountStr = amount.ToString("F2");
            int dotsLength = lineWidth - itemName.Length - amountStr.Length;
            
            if (dotsLength < 1)
            {
                itemName = itemName.Substring(0, lineWidth - amountStr.Length - 1);
                dotsLength = 1;
            }
            
            return itemName + new string('.', dotsLength) + amountStr;
        }

        private ThermalPrinterSettings GetThermalPrinterSettings()
        {
            return new ThermalPrinterSettings
            {
                PaperWidth = 58,
                CharactersPerLine = 32,
                SupportsBold = true,
                SupportsUnderline = true,
                SupportsGraphics = false
            };
        }

        private string ConvertAmountToWords(decimal amount)
        {
            // Implement number to words conversion
            // For now, return a simple format
            return $"Amount: {amount:C}";
        }

        private async Task<string> GetStaffNameAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Username ?? "System";
        }

        private string GetCurrentTerm()
        {
            // Implement your term logic
            var month = DateTime.Now.Month;
            return month <= 4 ? "Term 1" : month <= 8 ? "Term 2" : "Term 3";
        }
    }
}

