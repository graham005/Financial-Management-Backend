using Financial_management_backend.Services.Dtos.Reports;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Financial_management_backend.Services.Reports
{
    public interface IExcelReportGenerator
    {
        byte[] GenerateDailyCollectionsReport(DailyCollectionsReportDto data);
        byte[] GenerateRevenueSummaryReport(RevenueSummaryReportDto data);
        byte[] GenerateOutstandingFeesReport(OutstandingFeesReportDto data);
        byte[] GenerateFeeCollectionRateReport(FeeCollectionRateReportDto data);
        byte[] GeneratePaymentHistoryReport(PaymentHistoryReportDto data);
        byte[] GenerateItemTransactionReport(ItemTransactionReportDto data);
        byte[] GenerateStudentAccountStatement(StudentAccountStatementDto data);
    }

    public class ExcelReportGenerator : IExcelReportGenerator
    {
        public ExcelReportGenerator()
        {
            // Set EPPlus license context for EPPlus 8 and later
            ExcelPackage.License.SetNonCommercialPersonal("Destiny Junior Academy");
        }

        public byte[] GenerateDailyCollectionsReport(DailyCollectionsReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Daily Collections");

            // Title
            worksheet.Cells["A1"].Value = "Daily Collections Report";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"Date: {data.ReportDate:MMMM dd, yyyy}";
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Summary
            int row = 4;
            worksheet.Cells[row, 1].Value = "Total Collected:";
            worksheet.Cells[row, 2].Value = data.TotalCollected;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Total Transactions:";
            worksheet.Cells[row, 2].Value = data.TransactionCount;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            // Payment Method Breakdown
            if (data.ByPaymentMethod?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Payment Method Breakdown";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Payment Method";
                worksheet.Cells[row, 2].Value = "Amount";
                worksheet.Cells[row, 3].Value = "Count";
                FormatHeaderRow(worksheet, row, 1, 3);

                foreach (var method in data.ByPaymentMethod)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = method.PaymentMethod;
                    worksheet.Cells[row, 2].Value = method.Amount;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 3].Value = method.Count;
                }
            }

            // Fee Type Breakdown
            if (data.ByFeeType?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Fee Type Breakdown";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Fee Type";
                worksheet.Cells[row, 2].Value = "Amount";
                worksheet.Cells[row, 3].Value = "Count";
                FormatHeaderRow(worksheet, row, 1, 3);

                foreach (var feeType in data.ByFeeType)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = feeType.FeeType;
                    worksheet.Cells[row, 2].Value = feeType.Amount;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 3].Value = feeType.Count;
                }
            }

            // Transaction Details
            if (data.Transactions?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Transaction Details";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Student Name";
                worksheet.Cells[row, 2].Value = "Admission Number";
                worksheet.Cells[row, 3].Value = "Amount";
                worksheet.Cells[row, 4].Value = "Payment Method";
                worksheet.Cells[row, 5].Value = "Term";
                worksheet.Cells[row, 6].Value = "Year";
                FormatHeaderRow(worksheet, row, 1, 6);

                foreach (var txn in data.Transactions)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = txn.StudentName;
                    worksheet.Cells[row, 2].Value = txn.AdmissionNumber;
                    worksheet.Cells[row, 3].Value = txn.Amount;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = txn.PaymentMethod;
                    worksheet.Cells[row, 5].Value = txn.Term;
                    worksheet.Cells[row, 6].Value = txn.Year;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GenerateRevenueSummaryReport(RevenueSummaryReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Revenue Summary");

            // Title
            worksheet.Cells["A1"].Value = "Revenue Summary Report";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"{data.Term} {data.Year}";
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Total Revenue
            int row = 4;
            worksheet.Cells[row, 1].Value = "Total Revenue:";
            worksheet.Cells[row, 2].Value = data.TotalRevenue;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Size = 14;

            // Revenue by Fee Type
            if (data.ByFeeType?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Revenue by Fee Type";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Fee Type";
                worksheet.Cells[row, 2].Value = "Amount";
                worksheet.Cells[row, 3].Value = "Percentage";
                FormatHeaderRow(worksheet, row, 1, 3);

                foreach (var item in data.ByFeeType)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = item.FeeType;
                    worksheet.Cells[row, 2].Value = item.Amount;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 3].Value = item.Percentage / 100;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";
                }
            }

            // Revenue by Grade
            if (data.ByGrade?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Revenue by Grade";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Grade";
                worksheet.Cells[row, 2].Value = "Students";
                worksheet.Cells[row, 3].Value = "Total Amount";
                worksheet.Cells[row, 4].Value = "Avg per Student";
                FormatHeaderRow(worksheet, row, 1, 4);

                foreach (var grade in data.ByGrade)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = grade.GradeName;
                    worksheet.Cells[row, 2].Value = grade.StudentCount;
                    worksheet.Cells[row, 3].Value = grade.Amount;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = grade.AveragePerStudent;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                }
            }

            // Monthly Breakdown
            if (data.MonthlyBreakdown?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Monthly Breakdown";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Month";
                worksheet.Cells[row, 2].Value = "Amount";
                FormatHeaderRow(worksheet, row, 1, 2);

                foreach (var month in data.MonthlyBreakdown)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = month.MonthName;
                    worksheet.Cells[row, 2].Value = month.Amount;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GenerateOutstandingFeesReport(OutstandingFeesReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Outstanding Fees");

            // Title
            worksheet.Cells["A1"].Value = "Outstanding Fees Report";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"As of {data.Term} {data.Year}";
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Summary
            int row = 4;
            worksheet.Cells[row, 1].Value = "Total Outstanding:";
            worksheet.Cells[row, 2].Value = data.TotalOutstanding;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);

            row++;
            worksheet.Cells[row, 1].Value = "Students with Arrears:";
            worksheet.Cells[row, 2].Value = data.StudentsWithArrears;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            // By Grade Summary
            if (data.ByGrade?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Summary by Grade";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Grade";
                worksheet.Cells[row, 2].Value = "Students";
                worksheet.Cells[row, 3].Value = "Total Outstanding";
                worksheet.Cells[row, 4].Value = "Average";
                FormatHeaderRow(worksheet, row, 1, 4);

                foreach (var grade in data.ByGrade)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = grade.GradeName;
                    worksheet.Cells[row, 2].Value = grade.StudentsWithArrears;
                    worksheet.Cells[row, 3].Value = grade.TotalOutstanding;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = grade.AverageArrears;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                }
            }

            // Student Details
            if (data.Students?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Student Details";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Admission No";
                worksheet.Cells[row, 2].Value = "Student Name";
                worksheet.Cells[row, 3].Value = "Grade";
                worksheet.Cells[row, 4].Value = "Outstanding";
                worksheet.Cells[row, 5].Value = "Oldest Unpaid Term";
                worksheet.Cells[row, 6].Value = "Year";
                FormatHeaderRow(worksheet, row, 1, 6);

                foreach (var student in data.Students)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = student.AdmissionNumber;
                    worksheet.Cells[row, 2].Value = student.StudentName;
                    worksheet.Cells[row, 3].Value = student.Grade;
                    worksheet.Cells[row, 4].Value = student.OutstandingAmount;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 5].Value = student.OldestUnpaidTerm;
                    worksheet.Cells[row, 6].Value = student.OldestUnpaidYear;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GenerateFeeCollectionRateReport(FeeCollectionRateReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Collection Rate");

            // Title
            worksheet.Cells["A1"].Value = "Fee Collection Rate Report";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"{data.Term} {data.Year}";
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Overall Summary
            int row = 4;
            worksheet.Cells[row, 1].Value = "Expected Fees:";
            worksheet.Cells[row, 2].Value = data.ExpectedFees;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Collected Fees:";
            worksheet.Cells[row, 2].Value = data.CollectedFees;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Collection Rate:";
            worksheet.Cells[row, 2].Value = data.CollectionRate / 100;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00%";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Size = 14;
            worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);

            // By Grade
            if (data.ByGrade?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Collection Rate by Grade";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Grade";
                worksheet.Cells[row, 2].Value = "Students";
                worksheet.Cells[row, 3].Value = "Expected";
                worksheet.Cells[row, 4].Value = "Collected";
                worksheet.Cells[row, 5].Value = "Rate";
                FormatHeaderRow(worksheet, row, 1, 5);

                foreach (var grade in data.ByGrade)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = grade.GradeName;
                    worksheet.Cells[row, 2].Value = grade.StudentCount;
                    worksheet.Cells[row, 3].Value = grade.ExpectedFees;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = grade.CollectedFees;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 5].Value = grade.CollectionRate / 100;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "0.00%";

                    // Color code based on rate
                    if (grade.CollectionRate >= 80)
                        worksheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Green);
                    else if (grade.CollectionRate >= 50)
                        worksheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Orange);
                    else
                        worksheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Red);
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GeneratePaymentHistoryReport(PaymentHistoryReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Payment History");

            // Title
            worksheet.Cells["A1"].Value = "Payment History Report";
            worksheet.Cells["A1:H1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"{data.StartDate:MMM dd, yyyy} - {data.EndDate:MMM dd, yyyy}";
            worksheet.Cells["A2:H2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Summary
            int row = 4;
            worksheet.Cells[row, 1].Value = "Total Payments:";
            worksheet.Cells[row, 2].Value = data.TotalPayments;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Total Amount:";
            worksheet.Cells[row, 2].Value = data.TotalAmount;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            // Payment Details
            if (data.Payments?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Date";
                worksheet.Cells[row, 2].Value = "Receipt Number";
                worksheet.Cells[row, 3].Value = "Student";
                worksheet.Cells[row, 4].Value = "Grade";
                worksheet.Cells[row, 5].Value = "Amount";
                worksheet.Cells[row, 6].Value = "Method";
                worksheet.Cells[row, 7].Value = "Fee Type";
                worksheet.Cells[row, 8].Value = "Term/Year";
                worksheet.Cells[row, 9].Value = "Processed By";
                FormatHeaderRow(worksheet, row, 1, 9);

                foreach (var payment in data.Payments)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = payment.PaymentDate;
                    worksheet.Cells[row, 1].Style.Numberformat.Format = "mmm dd, yyyy";
                    worksheet.Cells[row, 2].Value = payment.ReceiptNumber;
                    worksheet.Cells[row, 3].Value = payment.StudentName;
                    worksheet.Cells[row, 4].Value = payment.Grade;
                    worksheet.Cells[row, 5].Value = payment.Amount;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 6].Value = payment.PaymentMethod;
                    worksheet.Cells[row, 7].Value = payment.FeeType;
                    worksheet.Cells[row, 8].Value = $"{payment.Term} {payment.Year}";
                    worksheet.Cells[row, 9].Value = payment.ProcessedBy;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GenerateItemTransactionReport(ItemTransactionReportDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Item Transactions");

            // Title
            worksheet.Cells["A1"].Value = "Item Transaction Report";
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2"].Value = $"{data.Term} {data.Year}";
            worksheet.Cells["A2:F2"].Merge = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Item Fulfillment Summary
            int row = 4;
            if (data.Items?.Any() == true)
            {
                worksheet.Cells[row, 1].Value = "Item Fulfillment Summary";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Item Name";
                worksheet.Cells[row, 2].Value = "Unit";
                worksheet.Cells[row, 3].Value = "Required";
                worksheet.Cells[row, 4].Value = "Received";
                worksheet.Cells[row, 5].Value = "Money Contributed";
                worksheet.Cells[row, 6].Value = "Fulfillment Rate";
                FormatHeaderRow(worksheet, row, 1, 6);

                foreach (var item in data.Items)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = item.ItemName;
                    worksheet.Cells[row, 2].Value = item.Unit;
                    worksheet.Cells[row, 3].Value = item.RequiredQuantity;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 4].Value = item.ReceivedQuantity;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 5].Value = item.MoneyContributed;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 6].Value = item.FulfillmentRate / 100;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "0.00%";

                    // Color code
                    if (item.FulfillmentRate >= 100)
                        worksheet.Cells[row, 6].Style.Font.Color.SetColor(Color.Green);
                    else if (item.FulfillmentRate >= 50)
                        worksheet.Cells[row, 6].Style.Font.Color.SetColor(Color.Orange);
                    else
                        worksheet.Cells[row, 6].Style.Font.Color.SetColor(Color.Red);
                }
            }

            // Student Item Status
            if (data.Students?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Student Item Status";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Admission No";
                worksheet.Cells[row, 2].Value = "Student Name";
                worksheet.Cells[row, 3].Value = "Grade";
                worksheet.Cells[row, 4].Value = "Status";
                worksheet.Cells[row, 5].Value = "Items Value";
                worksheet.Cells[row, 6].Value = "Money Contributed";
                FormatHeaderRow(worksheet, row, 1, 6);

                foreach (var student in data.Students)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = student.AdmissionNumber;
                    worksheet.Cells[row, 2].Value = student.StudentName;
                    worksheet.Cells[row, 3].Value = student.Grade;
                    worksheet.Cells[row, 4].Value = student.Status;
                    worksheet.Cells[row, 5].Value = student.ItemsValue;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 6].Value = student.MoneyContributed;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "$#,##0.00";

                    // Color code status
                    if (student.Status == "Complete")
                        worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                    else if (student.Status == "Partial")
                        worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Orange);
                    else
                        worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] GenerateStudentAccountStatement(StudentAccountStatementDto data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Account Statement");

            // Title
            worksheet.Cells["A1"].Value = "Student Account Statement";
            worksheet.Cells["A1:E1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Student Info
            int row = 3;
            worksheet.Cells[row, 1].Value = "Student Name:";
            worksheet.Cells[row, 2].Value = data.StudentName;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Admission Number:";
            worksheet.Cells[row, 2].Value = data.AdmissionNumber;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Current Grade:";
            worksheet.Cells[row, 2].Value = data.Grade;
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            // Account Summary
            row += 2;
            worksheet.Cells[row, 1].Value = "Total Fees Charged:";
            worksheet.Cells[row, 2].Value = data.TotalFeesCharged;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Total Paid:";
            worksheet.Cells[row, 2].Value = data.TotalPaid;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "Current Balance:";
            worksheet.Cells[row, 2].Value = data.CurrentBalance;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            if (data.CurrentBalance > 0)
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
            else
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Green);

            // Fee History by Term
            if (data.ByTerm?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Fee History by Term";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Term/Year";
                worksheet.Cells[row, 2].Value = "Charged";
                worksheet.Cells[row, 3].Value = "Paid";
                worksheet.Cells[row, 4].Value = "Outstanding";
                FormatHeaderRow(worksheet, row, 1, 4);

                foreach (var term in data.ByTerm)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = $"{term.Term} {term.Year}";
                    worksheet.Cells[row, 2].Value = term.FeesCharged;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 3].Value = term.AmountPaid;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = term.Outstanding;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";

                    if (term.Outstanding > 0)
                        worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                    else
                        worksheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                }
            }

            // Payment History
            if (data.PaymentHistory?.Any() == true)
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "Payment History";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;

                row++;
                worksheet.Cells[row, 1].Value = "Date";
                worksheet.Cells[row, 2].Value = "Receipt Number";
                worksheet.Cells[row, 3].Value = "Amount";
                worksheet.Cells[row, 4].Value = "Method";
                worksheet.Cells[row, 5].Value = "Description";
                FormatHeaderRow(worksheet, row, 1, 5);

                foreach (var payment in data.PaymentHistory)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = payment.PaymentDate;
                    worksheet.Cells[row, 1].Style.Numberformat.Format = "mmm dd, yyyy";
                    worksheet.Cells[row, 2].Value = payment.ReceiptNumber;
                    worksheet.Cells[row, 3].Value = payment.Amount;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 4].Value = payment.PaymentMethod;
                    worksheet.Cells[row, 5].Value = payment.Description;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        // Helper method to format header rows
        private void FormatHeaderRow(ExcelWorksheet worksheet, int row, int startCol, int endCol)
        {
            using (var range = worksheet.Cells[row, startCol, row, endCol])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }
    }
}