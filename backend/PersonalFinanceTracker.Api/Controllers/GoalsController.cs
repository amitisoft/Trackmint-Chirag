using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Goals;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/goals")]
public sealed class GoalsController(IGoalService goalService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<GoalResponse>> GetAll(CancellationToken cancellationToken) =>
        goalService.GetAllAsync(cancellationToken);

    [HttpPost]
    public Task<GoalResponse> Create([FromBody] CreateGoalRequest request, CancellationToken cancellationToken) =>
        goalService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<GoalResponse> Update(Guid id, [FromBody] UpdateGoalRequest request, CancellationToken cancellationToken) =>
        goalService.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/contribute")]
    public Task<GoalResponse> Contribute(Guid id, [FromBody] GoalContributionRequest request, CancellationToken cancellationToken) =>
        goalService.ContributeAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/withdraw")]
    public Task<GoalResponse> Withdraw(Guid id, [FromBody] GoalContributionRequest request, CancellationToken cancellationToken) =>
        goalService.WithdrawAsync(id, request, cancellationToken);
}
