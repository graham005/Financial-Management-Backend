namespace Financial_management_backend.Services.Dtos
{
    public class ThermalReceiptDto
    {
        // Receipt Header
        public ThermalReceiptHeader Header { get; set; }
        
        // Receipt Body
        public ThermalReceiptBody Body { get; set; }
        
        // Receipt Footer
        public ThermalReceiptFooter Footer { get; set; }
        
        // Thermal printer settings
        public ThermalPrinterSettings PrinterSettings { get; set; }
    }

    public class ThermalReceiptHeader
    {
        public string OrganizationName { get; set; }
        public string OrganizationAddress { get; set; }
        public string OrganizationPhone { get; set; }
        public string OrganizationEmail { get; set; }
        public string LogoBase64 { get; set; } // Optional logo for printers that support graphics
        public string ReceiptTitle { get; set; } // "PAYMENT RECEIPT" or "ITEM RECEIPT"
        public string ReceiptNumber { get; set; }
        public DateTime IssuedDate { get; set; }
    }

    public class ThermalReceiptBody
    {
        public ThermalCustomerInfo Customer { get; set; }
        public List<ThermalReceiptItem> Items { get; set; } = new();
        public ThermalReceiptTotals Totals { get; set; }
        public ThermalTransactionInfo Transaction { get; set; }
    }

    public class ThermalCustomerInfo
    {
        public string Name { get; set; }
        public string AdmissionNumber { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public string Contact { get; set; }
    }

    public class ThermalReceiptItem
    {
        public string Name { get; set; } // Max 20-25 chars for thermal printers
        public string ShortName { get; set; } // Truncated version
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public string FormattedLine { get; set; } // Pre-formatted line for printing
    }

    public class ThermalReceiptTotals
    {
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public string AmountInWords { get; set; }
    }

    public class ThermalTransactionInfo
    {
        public string PaymentMethod { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ProcessedBy { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
    }

    public class ThermalReceiptFooter
    {
        public string ThankYouMessage { get; set; }
        public string ContactInfo { get; set; }
        public List<string> AdditionalNotes { get; set; } = new();
        public string Signature { get; set; }
    }

    public class ThermalPrinterSettings
    {
        public int PaperWidth { get; set; } = 58; // 58mm or 80mm
        public int CharactersPerLine { get; set; } = 32; // Typical for 58mm
        public string Encoding { get; set; } = "UTF-8";
        public bool SupportsBold { get; set; } = true;
        public bool SupportsUnderline { get; set; } = true;
        public bool SupportsGraphics { get; set; } = false;
        public string LineBreak { get; set; } = "\n";
        public string CutCommand { get; set; } = "\x1d\x56\x41\x03"; // ESC/POS cut command
    }
}