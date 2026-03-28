using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class AccountAccessService(ApplicationDbContext dbContext) : IAccountAccessService
{
    public async Task<IReadOnlySet<Guid>> GetAccessibleAccountIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var owned = await dbContext.Accounts
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var memberOf = await dbContext.AccountMembers
            .Where(x => x.UserId == userId)
            .Select(x => x.AccountId)
            .ToListAsync(cancellationToken);

        return owned.Concat(memberOf).ToHashSet();
    }

    public async Task EnsureCanViewAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(userId, accountId, canManageAccount: false, canEditTransactions: false, cancellationToken);
    }

    public async Task EnsureCanEditTransactionsAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(userId, accountId, canManageAccount: false, canEditTransactions: true, cancellationToken);
    }

    public async Task EnsureCanManageAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(userId, accountId, canManageAccount: true, canEditTransactions: false, cancellationToken);
    }

    public async Task<AccountMemberRole> GetRoleAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var isOwner = await dbContext.Accounts.AnyAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken);
        if (isOwner)
        {
            return AccountMemberRole.Owner;
        }

        var membershipRole = await dbContext.AccountMembers
            .Where(x => x.AccountId == accountId && x.UserId == userId)
            .Select(x => (AccountMemberRole?)x.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (!membershipRole.HasValue)
        {
            throw new AppException("You do not have access to this account.", 403);
        }

        return membershipRole.Value;
    }

    private async Task EnsureRoleAsync(
        Guid userId,
        Guid accountId,
        bool canManageAccount,
        bool canEditTransactions,
        CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(userId, accountId, cancellationToken);
        if (canManageAccount && role != AccountMemberRole.Owner)
        {
            throw new AppException("Only account owner can manage shared access.", 403);
        }

        if (canEditTransactions && role == AccountMemberRole.Viewer)
        {
            throw new AppException("You only have viewer access for this account.", 403);
        }
    }
}
