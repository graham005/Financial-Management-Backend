using Financial_management_backend.Services.Dtos.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Financial_management_backend.Services.Reports
{
    public interface IPdfReportGenerator
    {
        byte[] GenerateDailyCollectionsReport(DailyCollectionsReportDto data);
        byte[] GenerateRevenueSummaryReport(RevenueSummaryReportDto data);
        byte[] GenerateOutstandingFeesReport(OutstandingFeesReportDto data);
        byte[] GenerateFeeCollectionRateReport(FeeCollectionRateReportDto data);
        byte[] GeneratePaymentHistoryReport(PaymentHistoryReportDto data);
        byte[] GenerateItemTransactionReport(ItemTransactionReportDto data);
        byte[] GenerateStudentAccountStatement(StudentAccountStatementDto data);
    }

    public class PdfReportGenerator : IPdfReportGenerator
    {
        public PdfReportGenerator()
        {
            // QuestPDF license configuration
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateDailyCollectionsReport(DailyCollectionsReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Report Title
                        column.Item().AlignCenter().Text("Daily Collections Report")
                            .FontSize(18).Bold();

                        column.Item().AlignCenter().Text($"Date: {data.ReportDate:MMMM dd, yyyy}")
                            .FontSize(12);

                        // Summary Section
                        column.Item().PaddingTop(15).Column(summary =>
                        {
                            summary.Item().Row(row =>
                            {
                                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                                {
                                    col.Item().Text("Total Collected").Bold();
                                    col.Item().Text($"{data.TotalCollected:C}").FontSize(16).SemiBold();
                                });

                                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                                {
                                    col.Item().Text("Total Transactions").Bold();
                                    col.Item().Text(data.TransactionCount.ToString()).FontSize(16).SemiBold();
                                });
                            });
                        });

                        // Payment Method Breakdown
                        if (data.ByPaymentMethod?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Payment Method Breakdown").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Payment Method").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Count").Bold();
                                });

                                foreach (var method in data.ByPaymentMethod)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(method.PaymentMethod);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{method.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(method.Count.ToString());
                                }
                            });
                        }

                        // Fee Type Breakdown
                        if (data.ByFeeType?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Fee Type Breakdown").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Fee Type").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Count").Bold();
                                });

                                foreach (var feeType in data.ByFeeType)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(feeType.FeeType);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{feeType.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(feeType.Count.ToString());
                                }
                            });
                        }

                        // Transaction List
                        if (data.Transactions?.Any() == true)
                        {
                            column.Item().PageBreak();
                            column.Item().Text("Transaction Details").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Student").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Adm No").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Method").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Term/Year").Bold();
                                });

                                foreach (var txn in data.Transactions)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(txn.StudentName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(txn.AdmissionNumber);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{txn.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(txn.PaymentMethod);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{txn.Term} {txn.Year}");
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateRevenueSummaryReport(RevenueSummaryReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Title
                        column.Item().AlignCenter().Text("Revenue Summary Report").FontSize(18).Bold();
                        column.Item().AlignCenter().Text($"{data.Term} {data.Year}").FontSize(12);

                        // Total Revenue
                        column.Item().PaddingTop(15).Background(Colors.Blue.Lighten4).Padding(15).Column(col =>
                        {
                            col.Item().Text("Total Revenue").FontSize(14).Bold();
                            col.Item().Text($"{data.TotalRevenue:C}").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                        });

                        // Revenue by Fee Type
                        if (data.ByFeeType?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Revenue by Fee Type").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Fee Type").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Percentage").Bold();
                                });

                                foreach (var item in data.ByFeeType)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.FeeType);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.Percentage:F2}%");
                                }
                            });
                        }

                        // Revenue by Grade
                        if (data.ByGrade?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Revenue by Grade").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Students").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Total").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Avg/Student").Bold();
                                });

                                foreach (var grade in data.ByGrade)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(grade.GradeName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(grade.StudentCount.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.AveragePerStudent:C}");
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateOutstandingFeesReport(OutstandingFeesReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("Outstanding Fees Report").FontSize(18).Bold();
                        column.Item().AlignCenter().Text($"As of {data.Term} {data.Year}").FontSize(12);

                        // Summary
                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Outstanding").Bold();
                                col.Item().Text($"{data.TotalOutstanding:C}").FontSize(16).FontColor(Colors.Red.Darken2);
                            });

                            row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Students with Arrears").Bold();
                                col.Item().Text(data.StudentsWithArrears.ToString()).FontSize(16).FontColor(Colors.Orange.Darken2);
                            });
                        });

                        // By Grade Summary
                        if (data.ByGrade?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Summary by Grade").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Students").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Total").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Average").Bold();
                                });

                                foreach (var grade in data.ByGrade)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(grade.GradeName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(grade.StudentsWithArrears.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.TotalOutstanding:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.AverageArrears:C}");
                                }
                            });
                        }

                        // Student Details
                        if (data.Students?.Any() == true)
                        {
                            column.Item().PageBreak();
                            column.Item().Text("Student Details").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Adm No").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Name").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Outstanding").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Since").Bold();
                                });

                                foreach (var student in data.Students)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.AdmissionNumber);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.StudentName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Grade);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{student.OutstandingAmount:C}").FontColor(Colors.Red.Darken1);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{student.OldestUnpaidTerm} {student.OldestUnpaidYear}");
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateFeeCollectionRateReport(FeeCollectionRateReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("Fee Collection Rate Report").FontSize(18).Bold();
                        column.Item().AlignCenter().Text($"{data.Term} {data.Year}").FontSize(12);

                        // Overall Summary
                        column.Item().PaddingTop(15).Background(Colors.Green.Lighten4).Padding(15).Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Expected Fees").Bold();
                                    c.Item().Text($"{data.ExpectedFees:C}").FontSize(14);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Collected").Bold();
                                    c.Item().Text($"{data.CollectedFees:C}").FontSize(14);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Collection Rate").Bold();
                                    c.Item().Text($"{data.CollectionRate:F2}%").FontSize(16).FontColor(Colors.Green.Darken2);
                                });
                            });
                        });

                        // By Grade
                        if (data.ByGrade?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Collection Rate by Grade").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Students").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Expected").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Collected").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Rate").Bold();
                                });

                                foreach (var grade in data.ByGrade)
                                {
                                    var color = grade.CollectionRate >= 80 ? Colors.Green.Medium :
                                               grade.CollectionRate >= 50 ? Colors.Orange.Medium :
                                               Colors.Red.Medium;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(grade.GradeName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(grade.StudentCount.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.ExpectedFees:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.CollectedFees:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{grade.CollectionRate:F2}%").FontColor(color);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GeneratePaymentHistoryReport(PaymentHistoryReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("Payment History Report").FontSize(18).Bold();
                        column.Item().AlignCenter().Text($"{data.StartDate:MMM dd, yyyy} - {data.EndDate:MMM dd, yyyy}").FontSize(12);

                        // Summary
                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments").Bold();
                                col.Item().Text(data.TotalPayments.ToString()).FontSize(14);
                            });

                            row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Amount").Bold();
                                col.Item().Text($"{data.TotalAmount:C}").FontSize(14);
                            });
                        });

                        // Payment List
                        if (data.Payments?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Date").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Receipt#").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Student").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Method").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Fee Type").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(3).Text("Term").Bold();
                                });

                                foreach (var payment in data.Payments)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{payment.PaymentDate:MMM dd}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(payment.ReceiptNumber).FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(payment.StudentName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(payment.Grade);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{payment.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(payment.PaymentMethod);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(payment.FeeType);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{payment.Term} {payment.Year}");
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateItemTransactionReport(ItemTransactionReportDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("Item Transaction Report").FontSize(18).Bold();
                        column.Item().AlignCenter().Text($"{data.Term} {data.Year}").FontSize(12);

                        // Item Fulfillment Summary
                        if (data.Items?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Item Fulfillment Summary").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Item").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Unit").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Required").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Received").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Money").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Rate").Bold();
                                });

                                foreach (var item in data.Items)
                                {
                                    var color = item.FulfillmentRate >= 100 ? Colors.Green.Medium :
                                               item.FulfillmentRate >= 50 ? Colors.Orange.Medium :
                                               Colors.Red.Medium;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ItemName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Unit);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.RequiredQuantity:F2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.ReceivedQuantity:F2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.MoneyContributed:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.FulfillmentRate:F1}%").FontColor(color);
                                }
                            });
                        }

                        // Student Status
                        if (data.Students?.Any() == true)
                        {
                            column.Item().PageBreak();
                            column.Item().Text("Student Item Status").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Adm No").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Name").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Grade").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Status").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Items Value").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Money").Bold();
                                });

                                foreach (var student in data.Students)
                                {
                                    var statusColor = student.Status == "Complete" ? Colors.Green.Medium :
                                                    student.Status == "Partial" ? Colors.Orange.Medium :
                                                    Colors.Red.Medium;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.AdmissionNumber);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.StudentName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Grade);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Status).FontColor(statusColor);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{student.ItemsValue:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{student.MoneyContributed:C}");
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateStudentAccountStatement(StudentAccountStatementDto data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().Text("Student Account Statement").FontSize(18).Bold();

                        // Student Info
                        column.Item().PaddingTop(15).Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Student Name:").Bold();
                                col.Item().Text(data.StudentName);
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Admission Number:").Bold();
                                col.Item().Text(data.AdmissionNumber);
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Current Grade:").Bold();
                                col.Item().Text(data.Grade);
                            });
                        });

                        // Account Summary
                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Charged").Bold();
                                col.Item().Text($"{data.TotalFeesCharged:C}").FontSize(14);
                            });
                            row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Paid").Bold();
                                col.Item().Text($"{data.TotalPaid:C}").FontSize(14);
                            });
                            row.RelativeItem().Background(data.CurrentBalance > 0 ? Colors.Red.Lighten4 : Colors.Grey.Lighten3).Padding(10).Column(col =>
                            {
                                col.Item().Text("Current Balance").Bold();
                                col.Item().Text($"{data.CurrentBalance:C}").FontSize(14).FontColor(data.CurrentBalance > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });
                        });

                        // Term-by-Term Breakdown
                        if (data.ByTerm?.Any() == true)
                        {
                            column.Item().PaddingTop(15).Text("Fee History by Term").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Term/Year").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Charged").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Paid").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Outstanding").Bold();
                                });

                                foreach (var term in data.ByTerm)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{term.Term} {term.Year}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{term.FeesCharged:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{term.AmountPaid:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                                        .Text($"{term.Outstanding:C}")
                                        .FontColor(term.Outstanding > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                                }
                            });
                        }

                        // Payment History
                        if (data.PaymentHistory?.Any() == true)
                        {
                            column.Item().PageBreak();
                            column.Item().Text("Payment History").FontSize(14).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Date").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Receipt Number").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("Amount").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Method").Bold();
                                    header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Description").Bold();
                                });

                                foreach (var payment in data.PaymentHistory)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{payment.PaymentDate:MMM dd, yyyy}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(payment.ReceiptNumber).FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{payment.Amount:C}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(payment.PaymentMethod);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(payment.Description);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        // Common header for all reports
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Your School Name").FontSize(16).Bold();
                    column.Item().Text("Financial Management System").FontSize(10);
                    column.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}").FontSize(8);
                });
            });
        }
    }
}