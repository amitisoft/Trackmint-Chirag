using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Accounts;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class AccountService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService,
    IBalanceService balanceService,
    IAuditService auditService) : IAccountService
{
    public async Task<IReadOnlyCollection<AccountResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accessibleAccountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var memberships = await dbContext.AccountMembers
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        var accounts = await dbContext.Accounts
            .Include(x => x.User)
            .Where(x => accessibleAccountIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return accounts.Select(x =>
        {
            var role = x.UserId == userId
                ? AccountMemberRole.Owner
                : memberships.TryGetValue(x.Id, out var membership)
                    ? membership.Role
                    : AccountMemberRole.Viewer;

            return x.ToResponse(role, x.UserId != userId, x.User?.DisplayName);
        }).ToArray();
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstBlank(request.Name, "Account name");
        ValidationGuard.AgainstNegativeAmount(request.OpeningBalance, "Opening balance");

        var account = new Account
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            OpeningBalance = request.OpeningBalance,
            CurrentBalance = request.OpeningBalance,
            InstitutionName = request.InstitutionName?.Trim()
        };

        await dbContext.Accounts.AddAsync(account, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "account_created", nameof(Account), account.Id, new { account.Name }, cancellationToken);
        return account.ToResponse();
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        await accountAccessService.EnsureCanManageAccountAsync(userId, id, cancellationToken);

        var account = await dbContext.Accounts
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        ValidationGuard.AgainstBlank(request.Name, "Account name");
        ValidationGuard.AgainstNegativeAmount(request.OpeningBalance, "Opening balance");

        account.Name = request.Name.Trim();
        account.Type = request.Type;
        account.OpeningBalance = request.OpeningBalance;
        account.InstitutionName = request.InstitutionName?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        await balanceService.RecalculateForUserAsync(userId, cancellationToken);
        await auditService.WriteAsync(userId, "account_updated", nameof(Account), account.Id, new { account.Name }, cancellationToken);

        return account.ToResponse(AccountMemberRole.Owner, false, account.User?.DisplayName);
    }

    public async Task TransferAsync(TransferFundsRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstNonPositiveAmount(request.Amount);

        await accountAccessService.EnsureCanEditTransactionsAsync(userId, request.FromAccountId, cancellationToken);
        await accountAccessService.EnsureCanEditTransactionsAsync(userId, request.ToAccountId, cancellationToken);

        var fromAccount = await dbContext.Accounts.SingleOrDefaultAsync(x => x.Id == request.FromAccountId, cancellationToken)
            ?? throw new NotFoundException("Source account not found.");
        var toAccount = await dbContext.Accounts.SingleOrDefaultAsync(x => x.Id == request.ToAccountId, cancellationToken)
            ?? throw new NotFoundException("Destination account not found.");

        if (fromAccount.Id == toAccount.Id)
        {
            throw new ValidationException("Transfer requires two different accounts.");
        }

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = fromAccount.Id,
            DestinationAccountId = toAccount.Id,
            Type = TransactionType.Transfer,
            Amount = request.Amount,
            TransactionDate = request.Date,
            Note = request.Note
        };

        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await balanceService.RecalculateForUserAsync(fromAccount.UserId, cancellationToken);
        if (toAccount.UserId != fromAccount.UserId)
        {
            await balanceService.RecalculateForUserAsync(toAccount.UserId, cancellationToken);
        }
        await auditService.WriteAsync(
            userId,
            "account_transfer",
            nameof(Transaction),
            transaction.Id,
            new { FromAccount = fromAccount.Name, ToAccount = toAccount.Name, request.Amount },
            cancellationToken);
    }
}
