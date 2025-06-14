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

        public JwtService(ApplicationDbContext dbContext, IConfiguration configuration) 
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<LoginReponse?> Authenticate(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password)) { return null; }

            return await GenerateJwtToken(user);

        }

        private async Task<LoginReponse> GenerateJwtToken(User user)
        {
            var issuer = _configuration["JwtConfig:Issuer"];
            var audience = _configuration["JwtConfig:Audience"];
            var key = _configuration["JwtConfig:Key"];
            var tokenValidityMins = _configuration.GetValue<int>("JwtConfig:ExpireTime");
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(tokenValidityMins);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    

                }),
                Expires = tokenExpiryTimeStamp,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
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
            var refreshTokenValidityMins = _configuration.GetValue<int>("JwtConfig:RefreshTokenValidityMins");
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expiry = DateTime.UtcNow.AddMinutes(refreshTokenValidityMins),
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
