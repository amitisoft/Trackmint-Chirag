using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Insights;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/insights")]
public sealed class InsightsController(IInsightsService insightsService) : ControllerBase
{
    [HttpGet("health-score")]
    public Task<FinancialHealthScoreResponse> GetHealthScore(CancellationToken cancellationToken) =>
        insightsService.GetHealthScoreAsync(cancellationToken);

    [HttpGet]
    public Task<IReadOnlyCollection<InsightCardResponse>> GetAll(CancellationToken cancellationToken) =>
        insightsService.GetInsightsAsync(cancellationToken);
}
