using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            
    }
}
