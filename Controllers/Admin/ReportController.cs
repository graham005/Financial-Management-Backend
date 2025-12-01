using Financial_management_backend.Services.Dtos.Reports;
using Financial_management_backend.Services.Helpers;
using Financial_management_backend.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Accountant")]
    public class ReportController(
        IReportService reportService,
        IPdfReportGenerator pdfGenerator,
        IExcelReportGenerator excelGenerator) : ControllerBase
    {
        private readonly IReportService _reportService = reportService;
        private readonly IPdfReportGenerator _pdfGenerator = pdfGenerator;
        private readonly IExcelReportGenerator _excelGenerator = excelGenerator;

        // ==================== DAILY COLLECTIONS REPORT ====================

        [HttpGet("daily-collections")]
        public async Task<IActionResult> GetDailyCollectionsReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateDailyCollectionsReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateDailyCollectionsReport(reportData);
                    var fileName = $"Daily_Collections_{reportData.ReportDate:yyyyMMdd}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Daily Collections",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.Transactions?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateDailyCollectionsReport(reportData);
                    var fileName = $"Daily_Collections_{reportData.ReportDate:yyyyMMdd}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Daily Collections",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.Transactions?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    // Return JSON
                    await _reportService.LogReportGenerationAsync(
                        "Daily Collections",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.Transactions?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== REVENUE SUMMARY REPORT ====================

        [HttpGet("revenue-summary")]
        public async Task<IActionResult> GetRevenueSummaryReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateRevenueSummaryReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateRevenueSummaryReport(reportData);
                    var fileName = $"Revenue_Summary_{reportData.Term}_{reportData.Year}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Revenue Summary",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateRevenueSummaryReport(reportData);
                    var fileName = $"Revenue_Summary_{reportData.Term}_{reportData.Year}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Revenue Summary",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Revenue Summary",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== OUTSTANDING FEES REPORT ====================

        [HttpGet("outstanding-fees")]
        public async Task<IActionResult> GetOutstandingFeesReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateOutstandingFeesReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateOutstandingFeesReport(reportData);
                    var fileName = $"Outstanding_Fees_{reportData.Term}_{reportData.Year}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Outstanding Fees",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.Students?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateOutstandingFeesReport(reportData);
                    var fileName = $"Outstanding_Fees_{reportData.Term}_{reportData.Year}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Outstanding Fees",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.Students?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Outstanding Fees",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.Students?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== FEE COLLECTION RATE REPORT ====================

        [HttpGet("collection-rate")]
        public async Task<IActionResult> GetFeeCollectionRateReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateFeeCollectionRateReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateFeeCollectionRateReport(reportData);
                    var fileName = $"Collection_Rate_{reportData.Term}_{reportData.Year}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Collection Rate",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateFeeCollectionRateReport(reportData);
                    var fileName = $"Collection_Rate_{reportData.Term}_{reportData.Year}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Collection Rate",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Collection Rate",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.ByGrade?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== PAYMENT HISTORY REPORT ====================

        [HttpGet("payment-history")]
        public async Task<IActionResult> GetPaymentHistoryReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GeneratePaymentHistoryReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GeneratePaymentHistoryReport(reportData);
                    var fileName = $"Payment_History_{reportData.StartDate:yyyyMMdd}_{reportData.EndDate:yyyyMMdd}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Payment History",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.Payments?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GeneratePaymentHistoryReport(reportData);
                    var fileName = $"Payment_History_{reportData.StartDate:yyyyMMdd}_{reportData.EndDate:yyyyMMdd}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Payment History",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.Payments?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Payment History",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.Payments?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== ITEM TRANSACTION REPORT ====================

        [HttpGet("item-transactions")]
        public async Task<IActionResult> GetItemTransactionReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateItemTransactionReportAsync(request);

                if (request.Format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateItemTransactionReport(reportData);
                    var fileName = $"Item_Transactions_{reportData.Term}_{reportData.Year}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Item Transactions",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "Excel",
                        reportData.Students?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (request.Format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateItemTransactionReport(reportData);
                    var fileName = $"Item_Transactions_{reportData.Term}_{reportData.Year}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Item Transactions",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "PDF",
                        reportData.Students?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Item Transactions",
                        userId.Value,
                        JsonSerializer.Serialize(request),
                        "JSON",
                        reportData.Students?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== STUDENT ACCOUNT STATEMENT ====================

        [HttpGet("student-statement/{studentId}")]
        public async Task<IActionResult> GetStudentAccountStatement(Guid studentId, [FromQuery] string? format)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                var reportData = await _reportService.GenerateStudentAccountStatementAsync(studentId);

                if (format?.ToUpper() == "EXCEL")
                {
                    var excelBytes = _excelGenerator.GenerateStudentAccountStatement(reportData);
                    var fileName = $"Account_Statement_{reportData.AdmissionNumber}_{DateTime.Now:yyyyMMdd}.xlsx";

                    await _reportService.LogReportGenerationAsync(
                        "Student Account Statement",
                        userId.Value,
                        JsonSerializer.Serialize(new { studentId }),
                        "Excel",
                        reportData.PaymentHistory?.Count ?? 0
                    );

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (format?.ToUpper() == "PDF")
                {
                    var pdfBytes = _pdfGenerator.GenerateStudentAccountStatement(reportData);
                    var fileName = $"Account_Statement_{reportData.AdmissionNumber}_{DateTime.Now:yyyyMMdd}.pdf";

                    await _reportService.LogReportGenerationAsync(
                        "Student Account Statement",
                        userId.Value,
                        JsonSerializer.Serialize(new { studentId }),
                        "PDF",
                        reportData.PaymentHistory?.Count ?? 0
                    );

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    await _reportService.LogReportGenerationAsync(
                        "Student Account Statement",
                        userId.Value,
                        JsonSerializer.Serialize(new { studentId }),
                        "JSON",
                        reportData.PaymentHistory?.Count ?? 0
                    );

                    return Ok(reportData);
                }
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating report: {ex.Message}" });
            }
        }

        // ==================== BATCH STUDENT STATEMENTS ====================

        [HttpPost("batch-student-statements")]
        public async Task<IActionResult> GenerateBatchStudentStatements([FromBody] BatchStatementRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                    return Unauthorized("User ID not found in token");

                if (request.StudentIds == null || request.StudentIds.Count == 0)
                    return BadRequest("At least one student ID is required");

                // For now, we'll return a ZIP file containing all PDFs
                // You could use a library like System.IO.Compression for this
                
                var statements = new List<StudentAccountStatementDto>();

                foreach (var studentId in request.StudentIds)
                {
                    try
                    {
                        var statement = await _reportService.GenerateStudentAccountStatementAsync(studentId);
                        statements.Add(statement);
                    }
                    catch
                    {
                        // Skip students that fail
                        continue;
                    }
                }

                await _reportService.LogReportGenerationAsync(
                    "Batch Student Statements",
                    userId.Value,
                    JsonSerializer.Serialize(request),
                    "JSON",
                    statements.Count
                );

                return Ok(new
                {
                    Message = $"Generated {statements.Count} statements",
                    Statements = statements
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating batch statements: {ex.Message}" });
            }
        }

        // ==================== REPORT SUMMARY ====================

        [HttpGet("summary")]
        public async Task<IActionResult> GetReportsSummary([FromQuery] ReportRequestDto request)
        {
            try
            {
                // Generate a quick summary of all key metrics
                var dailyCollections = await _reportService.GenerateDailyCollectionsReportAsync(new ReportRequestDto
                {
                    StartDate = DateTime.Today
                });

                var revenueSummary = await _reportService.GenerateRevenueSummaryReportAsync(request);
                var outstandingFees = await _reportService.GenerateOutstandingFeesReportAsync(request);
                var collectionRate = await _reportService.GenerateFeeCollectionRateReportAsync(request);

                return Ok(new
                {
                    DailyCollections = new
                    {
                        dailyCollections.TotalCollected,
                        dailyCollections.TransactionCount
                    },
                    Revenue = new
                    {
                        revenueSummary.TotalRevenue,
                        revenueSummary.Term,
                        revenueSummary.Year
                    },
                    Outstanding = new
                    {
                        outstandingFees.TotalOutstanding,
                        outstandingFees.StudentsWithArrears
                    },
                    CollectionRate = new
                    {
                        collectionRate.CollectionRate,
                        collectionRate.ExpectedFees,
                        collectionRate.CollectedFees
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating summary: {ex.Message}" });
            }
        }
    }

    // Request DTO for batch statements
    public class BatchStatementRequest
    {
        public List<Guid> StudentIds { get; set; } = [];
        public string Format { get; set; } = "PDF";
    }
}