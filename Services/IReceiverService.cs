namespace TotkBot.Services;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}
