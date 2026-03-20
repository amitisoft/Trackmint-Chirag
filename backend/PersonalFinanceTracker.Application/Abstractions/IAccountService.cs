using PersonalFinanceTracker.Application.DTOs.Accounts;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IAccountService
{
    Task<IReadOnlyCollection<AccountResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken);
    Task TransferAsync(TransferFundsRequest request, CancellationToken cancellationToken);
}
