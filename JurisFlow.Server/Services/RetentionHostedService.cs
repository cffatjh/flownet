using Microsoft.Extensions.Hosting;

namespace JurisFlow.Server.Services
{
    public class RetentionHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RetentionHostedService> _logger;

        public RetentionHostedService(IServiceProvider serviceProvider, ILogger<RetentionHostedService> logger)
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
                    using var scope = _serviceProvider.CreateScope();
                    var retention = scope.ServiceProvider.GetRequiredService<RetentionService>();
                    await retention.ApplyRetentionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Retention cleanup failed");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
