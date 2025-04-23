using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            
    }
}
