using PersonalFinanceTracker.Application.DTOs.Accounts;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IAccountMembershipService
{
    Task<IReadOnlyCollection<AccountMemberResponse>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken);
    Task InviteAsync(Guid accountId, InviteAccountMemberRequest request, CancellationToken cancellationToken);
    Task UpdateMemberRoleAsync(Guid accountId, Guid memberUserId, UpdateAccountMemberRoleRequest request, CancellationToken cancellationToken);
}
