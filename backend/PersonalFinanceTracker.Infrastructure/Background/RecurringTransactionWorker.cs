using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalFinanceTracker.Application.Abstractions;

namespace PersonalFinanceTracker.Infrastructure.Background;

public sealed class RecurringTransactionWorker(
    IServiceProvider serviceProvider,
    ILogger<RecurringTransactionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IRecurringTransactionService>();
                await service.ProcessDueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recurring transaction worker failed.");
            }
        }
    }
}
