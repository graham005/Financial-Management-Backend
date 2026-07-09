using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financial_management_backend.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StudentPromotionController(IStudentPromotionService promotionService) : ControllerBase
    {
        private readonly IStudentPromotionService _promotionService = promotionService;

        [HttpGet("preview")]
        public async Task<IActionResult> GetPromotionPreview()
        {
            try
            {
                var preview = await _promotionService.GetPromotionPreviewAsync();
                return Ok(preview);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error generating preview: {ex.Message}" });
            }
        }

        [HttpPost("promote")]
        public async Task<IActionResult> PromoteStudents([FromBody] PromoteStudentsRequest request)
        {
            try
            {
                if (request.StudentIds == null || request.StudentIds.Count == 0)
                {
                    return BadRequest(new { Message = "No students selected for promotion" });
                }

                var result = await _promotionService.PromoteStudentsAsync(request.StudentIds);

                return Ok(new
                {
                    Message = $"Successfully promoted {result.PromotedStudents.Count} student(s)",
                    Result = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error promoting students: {ex.Message}" });
            }
        }
    }

    public class PromoteStudentsRequest
    {
        public List<Guid> StudentIds { get; set; }
    }
}