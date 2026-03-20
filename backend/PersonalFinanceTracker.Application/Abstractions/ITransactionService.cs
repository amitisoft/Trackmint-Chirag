using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Application.DTOs.Transactions;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface ITransactionService
{
    Task<PagedResult<TransactionResponse>> GetAllAsync(TransactionQueryRequest request, CancellationToken cancellationToken);
    Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken);
    Task<TransactionResponse> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
