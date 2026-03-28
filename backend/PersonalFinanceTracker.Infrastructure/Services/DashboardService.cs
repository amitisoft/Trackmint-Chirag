using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Dashboard;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class DashboardService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var accounts = await dbContext.Accounts
            .Where(x => accountIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var monthTransactions = await dbContext.Transactions
            .Include(x => x.Category)
            .Include(x => x.Account)
            .Where(x =>
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))) &&
                x.TransactionDate >= monthStart &&
                x.TransactionDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var currentMonthIncome = monthTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var currentMonthExpense = monthTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

        var budgets = await dbContext.Budgets
            .Include(x => x.Category)
            .Where(x => x.UserId == userId && x.Month == today.Month && x.Year == today.Year)
            .ToListAsync(cancellationToken);

        var budgetCards = budgets.Select(budget =>
        {
            var actual = monthTransactions
                .Where(x => x.Type == TransactionType.Expense && x.CategoryId == budget.CategoryId)
                .Sum(x => x.Amount);

            return new BudgetProgressCard
            {
                Id = budget.Id,
                Category = budget.Category?.Name ?? string.Empty,
                BudgetAmount = budget.Amount,
                ActualAmount = actual,
                UtilizationPercent = budget.Amount == 0 ? 0 : Math.Round((actual / budget.Amount) * 100, 2),
                Color = budget.Category?.Color ?? "#3b82f6"
            };
        }).OrderByDescending(x => x.UtilizationPercent).Take(5).ToArray();

        var spendingByCategory = monthTransactions
            .Where(x => x.Type == TransactionType.Expense && x.Category is not null)
            .GroupBy(x => new { x.Category!.Name, x.Category.Color })
            .Select(group => new ChartSlice
            {
                Label = group.Key.Name,
                Value = group.Sum(x => x.Amount),
                Color = group.Key.Color
            })
            .OrderByDescending(x => x.Value)
            .ToArray();

        var recentTransactions = await dbContext.Transactions
            .Include(x => x.Category)
            .Include(x => x.Account)
            .Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value)))
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(6)
            .Select(x => new RecentTransactionItem
            {
                Id = x.Id,
                Merchant = string.IsNullOrWhiteSpace(x.Merchant) ? "Manual Entry" : x.Merchant!,
                Category = x.Category != null ? x.Category.Name : "Transfer",
                Account = x.Account != null ? x.Account.Name : string.Empty,
                Type = x.Type.ToString(),
                Amount = x.Amount,
                Date = x.TransactionDate
            })
            .ToArrayAsync(cancellationToken);

        var upcomingRecurring = await dbContext.RecurringTransactions
            .Where(x =>
                !x.IsPaused &&
                x.NextRunDate >= today &&
                x.NextRunDate <= today.AddDays(30) &&
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))))
            .OrderBy(x => x.NextRunDate)
            .Take(6)
            .Select(x => new UpcomingRecurringItem
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                NextRunDate = x.NextRunDate,
                Frequency = x.Frequency.ToString()
            })
            .ToArrayAsync(cancellationToken);

        var goals = await dbContext.Goals
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.TargetDate)
            .Take(5)
            .Select(x => new GoalProgressItem
            {
                Id = x.Id,
                Name = x.Name,
                CurrentAmount = x.CurrentAmount,
                TargetAmount = x.TargetAmount,
                ProgressPercent = x.TargetAmount == 0 ? 0 : Math.Round((x.CurrentAmount / x.TargetAmount) * 100, 2),
                TargetDate = x.TargetDate,
                Color = x.Color
            })
            .ToArrayAsync(cancellationToken);

        return new DashboardSummaryResponse
        {
            CurrentMonthIncome = currentMonthIncome,
            CurrentMonthExpense = currentMonthExpense,
            NetBalance = accounts.Sum(x => x.CurrentBalance),
            BudgetProgressCards = budgetCards,
            SpendingByCategory = spendingByCategory,
            IncomeExpenseTrend = await BuildTrendAsync(accountIds, today, cancellationToken),
            RecentTransactions = recentTransactions,
            UpcomingRecurringPayments = upcomingRecurring,
            SavingsGoals = goals
        };
    }

    private async Task<IReadOnlyCollection<TrendPoint>> BuildTrendAsync(IReadOnlySet<Guid> accountIds, DateOnly today, CancellationToken cancellationToken)
    {
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var transactions = await dbContext.Transactions
            .Where(x =>
                x.TransactionDate >= firstMonth &&
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))))
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, 6)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => new TrendPoint
            {
                Label = month.ToString("MMM yy"),
                Income = transactions.Where(x => x.TransactionDate.Year == month.Year && x.TransactionDate.Month == month.Month && x.Type == TransactionType.Income).Sum(x => x.Amount),
                Expense = transactions.Where(x => x.TransactionDate.Year == month.Year && x.TransactionDate.Month == month.Month && x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .ToArray();
    }
}
