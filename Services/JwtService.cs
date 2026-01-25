using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Financial_management_backend.Services
{
    public class JwtService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _tokenValidityMins;
        private readonly int _refreshTokenValidityMins;

        public JwtService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;

            // Read from environment variables first, fall back to configuration
            _jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["JwtConfig:Key"];
            _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["JwtConfig:Issuer"];
            _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["JwtConfig:Audience"];

            // Parse numeric values with fallback
            if (!int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"), out _tokenValidityMins))
            {
                _tokenValidityMins = _configuration.GetValue<int>("JwtConfig:ExpireTime", 60);
            }

            if (!int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRE_MINUTES"), out _refreshTokenValidityMins))
            {
                _refreshTokenValidityMins = _configuration.GetValue<int>("JwtConfig:RefreshTokenValidityMins", 10080);
            }

            // Validate that we have the required configuration
            if (string.IsNullOrEmpty(_jwtKey))
                throw new InvalidOperationException("JWT Key is not configured");
            if (string.IsNullOrEmpty(_jwtIssuer))
                throw new InvalidOperationException("JWT Issuer is not configured");
            if (string.IsNullOrEmpty(_jwtAudience))
                throw new InvalidOperationException("JWT Audience is not configured");
        }

        public async Task<LoginReponse?> Authenticate(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return null;
            }

            return await GenerateJwtToken(user);
        }

        private async Task<LoginReponse> GenerateJwtToken(User user)
        {
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(_tokenValidityMins);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                }),
                Expires = tokenExpiryTimeStamp,
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtKey)),
                    SecurityAlgorithms.HmacSha512Signature),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            return new LoginReponse
            {
                AccessToken = accessToken,
                Email = user.Email,
                ExpiresIn = (int)tokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds,
                RefreshToken = await GenerateRefreshToken(user.Id),
                Role = user.Role
            };
        }

        private async Task<string> GenerateRefreshToken(Guid userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expiry = DateTime.UtcNow.AddMinutes(_refreshTokenValidityMins),
                UserId = userId
            };

            await _dbContext.RefreshTokens.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken.Token;
        }

        public async Task<LoginReponse?> ValidateRefreshToken(string token)
        {
            var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || refreshToken.Expiry < DateTime.UtcNow)
            {
                return null;
            }

            _dbContext.RefreshTokens.Remove(refreshToken);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);
            if (user == null) return null;

            return await GenerateJwtToken(user);
        }
    }
}
