using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class GradeHistoryController(IStudentGradeHistoryService gradeHistoryService) : ControllerBase
    {
        private readonly IStudentGradeHistoryService _gradeHistoryService = gradeHistoryService;

        [HttpPost("backfill-all")]
        public async Task<IActionResult> BackfillAllStudents()
        {
            try
            {
                await _gradeHistoryService.BackfillAllStudentsGradeHistoryAsync();
                return Ok(new { Message = "Grade history calculated successfully for all students" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating grade history: {ex.Message}");
            }
        }

        [HttpPost("backfill/{studentId}")]
        public async Task<IActionResult> BackfillStudent(Guid studentId)
        {
            try
            {
                await _gradeHistoryService.BackfillStudentGradeHistoryAsync(studentId);
                return Ok(new { Message = $"Grade history calculated successfully for student {studentId}" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating grade history: {ex.Message}");
            }
        }

        [HttpGet("{studentId}/history")]
        public async Task<IActionResult> GetStudentGradeHistory(Guid studentId)
        {
            try
            {
                var history = await _gradeHistoryService.GetCompleteGradeHistoryAsync(studentId);

                return Ok(new
                {
                    StudentId = studentId,
                    History = history.Select(h => new
                    {
                        h.Term,
                        h.Year,
                        h.GradeId,
                        h.GradeName,
                        h.Level
                    })
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving grade history: {ex.Message}");
            }
        }
    }
}