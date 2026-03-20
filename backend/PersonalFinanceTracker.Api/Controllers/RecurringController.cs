using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Recurring;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recurring")]
public sealed class RecurringController(IRecurringTransactionService recurringTransactionService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<RecurringTransactionResponse>> GetAll(CancellationToken cancellationToken) =>
        recurringTransactionService.GetAllAsync(cancellationToken);

    [HttpPost]
    public Task<RecurringTransactionResponse> Create([FromBody] CreateRecurringTransactionRequest request, CancellationToken cancellationToken) =>
        recurringTransactionService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<RecurringTransactionResponse> Update(Guid id, [FromBody] UpdateRecurringTransactionRequest request, CancellationToken cancellationToken) =>
        recurringTransactionService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await recurringTransactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
