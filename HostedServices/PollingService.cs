using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TotkBot.Services;

namespace TotkBot.HostedServices;

public class PollingService : BackgroundService
{
    private readonly ILogger<PollingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PollingService(ILogger<PollingService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<IReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling failed");
                // TODO replace with Polly
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}