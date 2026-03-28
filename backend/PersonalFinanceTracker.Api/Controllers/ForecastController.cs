using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Forecast;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/forecast")]
public sealed class ForecastController(IForecastService forecastService) : ControllerBase
{
    [HttpGet("month")]
    public Task<ForecastMonthResponse> GetMonth(CancellationToken cancellationToken) =>
        forecastService.GetMonthForecastAsync(cancellationToken);

    [HttpGet("daily")]
    public Task<IReadOnlyCollection<ForecastDailyPoint>> GetDaily(CancellationToken cancellationToken) =>
        forecastService.GetDailyForecastAsync(cancellationToken);
}
