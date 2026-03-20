using PersonalFinanceTracker.Application.DTOs.Recurring;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IRecurringTransactionService
{
    Task<IReadOnlyCollection<RecurringTransactionResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RecurringTransactionResponse> CreateAsync(CreateRecurringTransactionRequest request, CancellationToken cancellationToken);
    Task<RecurringTransactionResponse> UpdateAsync(Guid id, UpdateRecurringTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task ProcessDueItemsAsync(CancellationToken cancellationToken);
}
