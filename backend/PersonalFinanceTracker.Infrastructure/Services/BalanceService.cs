using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class BalanceService(ApplicationDbContext dbContext) : IBalanceService
{
    public async Task RecalculateForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var accountIds = accounts.Keys.ToHashSet();

        foreach (var account in accounts.Values)
        {
            account.CurrentBalance = account.OpeningBalance;
        }

        var transactions = await dbContext.Transactions
            .Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value)))
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            if (!accounts.TryGetValue(transaction.AccountId, out var sourceAccount))
            {
                continue;
            }

            switch (transaction.Type)
            {
                case TransactionType.Income:
                    sourceAccount.CurrentBalance += transaction.Amount;
                    break;
                case TransactionType.Expense:
                    sourceAccount.CurrentBalance -= transaction.Amount;
                    break;
                case TransactionType.Transfer:
                    sourceAccount.CurrentBalance -= transaction.Amount;

                    if (transaction.DestinationAccountId.HasValue &&
                        accounts.TryGetValue(transaction.DestinationAccountId.Value, out var destinationAccount))
                    {
                        destinationAccount.CurrentBalance += transaction.Amount;
                    }

                    break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
