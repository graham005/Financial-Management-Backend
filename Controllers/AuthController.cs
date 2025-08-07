using Financial_management_backend.Data;
using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService) =>
            _jwtService = jwtService;

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<LoginReponse>> Login(LoginRequest loginRequest)
        {
            var result = await _jwtService.Authenticate(loginRequest);
            if (result == null)
                return Unauthorized();

            return result;
        }

        [HttpPost("Refresh")]
        public async Task<ActionResult<LoginReponse>> Refresh([FromBody] RefreshRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Invalid Token");
            
            var result = await _jwtService.ValidateRefreshToken(request.Token);
            return result != null ? result : Unauthorized();
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<IActionResult> GetCurrentUser([FromServices] ApplicationDbContext dbContext)
        {
            // Extract email from JWT claims
            var email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Email claim not found in token.");

            // Query the user from the database
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound("User not found.");

            // Return user details (excluding sensitive info like password)
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }
    }
}
