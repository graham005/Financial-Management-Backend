using Financial_management_backend.Data;

namespace Financial_management_backend.Services.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task DeleteExpiredTokensAsync();
    }
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _context.RefreshTokens.Where(rt => rt.Expiry < now);

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
