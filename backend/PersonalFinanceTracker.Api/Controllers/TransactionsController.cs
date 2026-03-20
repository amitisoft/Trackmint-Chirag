using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Application.DTOs.Transactions;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<TransactionResponse>> GetAll([FromQuery] TransactionQueryRequest request, CancellationToken cancellationToken) =>
        transactionService.GetAllAsync(request, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<TransactionResponse> GetById(Guid id, CancellationToken cancellationToken) =>
        transactionService.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public Task<TransactionResponse> Create([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken) =>
        transactionService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<TransactionResponse> Update(Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken) =>
        transactionService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await transactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
