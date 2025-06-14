using Financial_management_backend.Services.Repositories;

namespace Financial_management_backend.Services.BackgroundServices
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting cleanup of expired refresh tokens.");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                        await refreshTokenRepository.DeleteExpiredTokensAsync();
                    }

                    _logger.LogInformation("Expired refresh tokens cleanup completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up expired refresh tokens.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
