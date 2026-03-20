using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Budgets;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public sealed class BudgetsController(IBudgetService budgetService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<BudgetResponse>> GetAll([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken) =>
        budgetService.GetAllAsync(month, year, cancellationToken);

    [HttpPost]
    public Task<BudgetResponse> Create([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken) =>
        budgetService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<BudgetResponse> Update(Guid id, [FromBody] UpdateBudgetRequest request, CancellationToken cancellationToken) =>
        budgetService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await budgetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
