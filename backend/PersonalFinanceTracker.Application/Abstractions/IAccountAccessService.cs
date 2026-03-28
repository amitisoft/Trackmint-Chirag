using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IAccountAccessService
{
    Task<IReadOnlySet<Guid>> GetAccessibleAccountIdsAsync(Guid userId, CancellationToken cancellationToken);
    Task EnsureCanViewAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task EnsureCanEditTransactionsAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task EnsureCanManageAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task<AccountMemberRole> GetRoleAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
}
