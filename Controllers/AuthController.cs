using Financial_management_backend.Data;
using Financial_management_backend.Services;
using Financial_management_backend.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(JwtService jwtService, ApplicationDbContext dbContext) : ControllerBase
    {
        private readonly JwtService _jwtService = jwtService;
        private readonly ApplicationDbContext _dbContext = dbContext;

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<LoginReponse>> Login(LoginRequest loginRequest)
        {
            var result = await _jwtService.Authenticate(loginRequest);
            if (result == null)
                return Unauthorized(new { Message = "Invalid email or password" });

            return result;
        }

        [HttpPost("Refresh")]
        public async Task<ActionResult<LoginReponse>> Refresh([FromBody] RefreshRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { Message = "Invalid token" });
            
            var result = await _jwtService.ValidateRefreshToken(request.Token);
            return result != null ? result : Unauthorized(new { Message = "Invalid or expired refresh token" });
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Extract email from JWT claims
            var email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { Message = "Email claim not found in token" });

            // Query the user from the database
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Return user details (excluding sensitive info like password)
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get user ID from JWT claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "User ID not found in token" });

                // Get user from database
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
                    return BadRequest(new { Message = "Current password is incorrect" });

                // Check if new password is same as current password
                if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.Password))
                    return BadRequest(new { Message = "New password cannot be the same as current password" });

                // Update password
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                // Invalidate all existing refresh tokens for this user (force re-login on all devices)
                var userRefreshTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == userId)
                    .ToListAsync();

                if (userRefreshTokens.Count != 0)
                {
                    _dbContext.RefreshTokens.RemoveRange(userRefreshTokens);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Password changed successfully. Please login again with your new password.",
                    RequiresRelogin = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error changing password: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return BadRequest(new { Message = "Refresh token is required" });

                // Get user ID from JWT claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "User ID not found in token" });

                // Remove the specific refresh token
                var refreshToken = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

                if (refreshToken != null)
                {
                    _dbContext.RefreshTokens.Remove(refreshToken);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error logging out: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAllDevices()
        {
            try
            {
                // Get user ID from JWT claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { Message = "User ID not found in token" });

                // Remove all refresh tokens for this user
                var userRefreshTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == userId)
                    .ToListAsync();

                if (userRefreshTokens.Count != 0)
                {
                    _dbContext.RefreshTokens.RemoveRange(userRefreshTokens);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new
                {
                    Message = "Logged out from all devices successfully",
                    DevicesLoggedOut = userRefreshTokens.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error logging out from all devices: {ex.Message}" });
            }
        }
    }

    // Request model for logout
    public class LogoutRequest
    {
        public string RefreshToken { get; set; }
    }
}
