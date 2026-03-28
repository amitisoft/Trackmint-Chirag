using PersonalFinanceTracker.Application.DTOs.Forecast;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IForecastService
{
    Task<ForecastMonthResponse> GetMonthForecastAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ForecastDailyPoint>> GetDailyForecastAsync(CancellationToken cancellationToken);
}
