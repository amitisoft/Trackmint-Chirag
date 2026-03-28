using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Forecast;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class ForecastService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService) : IForecastService
{
    public async Task<ForecastMonthResponse> GetMonthForecastAsync(CancellationToken cancellationToken)
    {
        var model = await BuildForecastModelAsync(cancellationToken);
        return new ForecastMonthResponse
        {
            CurrentBalance = model.CurrentBalance,
            ProjectedEndOfMonthBalance = model.ProjectedEndBalance,
            SafeToSpend = model.SafeToSpend,
            UpcomingKnownTransactions = model.UpcomingItems,
            RiskWarnings = model.RiskWarnings
        };
    }

    public async Task<IReadOnlyCollection<ForecastDailyPoint>> GetDailyForecastAsync(CancellationToken cancellationToken)
    {
        var model = await BuildForecastModelAsync(cancellationToken);
        return model.DailyPoints;
    }

    private async Task<ForecastComputation> BuildForecastModelAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var historyStart = today.AddMonths(-3);

        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);

        var accounts = await dbContext.Accounts
            .Where(x => accountIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var historyTransactions = await dbContext.Transactions
            .Where(x =>
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))) &&
                x.TransactionDate >= historyStart &&
                x.TransactionDate <= today)
            .ToArrayAsync(cancellationToken);

        var recurring = await dbContext.RecurringTransactions
            .Where(x =>
                !x.IsPaused &&
                x.AutoCreateTransaction &&
                x.NextRunDate <= monthEnd &&
                (x.EndDate == null || x.EndDate >= today) &&
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))))
            .OrderBy(x => x.NextRunDate)
            .ToArrayAsync(cancellationToken);

        var currentBalance = accounts.Sum(x => x.CurrentBalance);
        var dailyAverageNet = historyTransactions.Sum(item => CalculateNetEffect(item, accountIds)) / Math.Max((today.DayNumber - historyStart.DayNumber) + 1, 1);

        var knownNetByDay = BuildRecurringNetByDay(recurring, accountIds, today, monthEnd);
        var upcomingItems = BuildUpcomingItems(recurring, accountIds, today, monthEnd);

        var runningBalance = currentBalance;
        var dailyPoints = new List<ForecastDailyPoint>();
        var negativeBalanceDates = new List<DateOnly>();

        for (var date = today; date <= monthEnd; date = date.AddDays(1))
        {
            if (date > today)
            {
                runningBalance += dailyAverageNet;
            }

            if (knownNetByDay.TryGetValue(date, out var knownDelta))
            {
                runningBalance += knownDelta;
            }

            if (runningBalance < 0)
            {
                negativeBalanceDates.Add(date);
            }

            dailyPoints.Add(new ForecastDailyPoint
            {
                Date = date,
                ProjectedBalance = Math.Round(runningBalance, 2)
            });
        }

        var averageMonthlyExpense = Math.Abs(historyTransactions
            .Where(x => x.Type == TransactionType.Expense)
            .Sum(x => CalculateNetEffect(x, accountIds))) / 3m;
        var buffer = averageMonthlyExpense * 0.2m;
        var projectedEnd = dailyPoints.LastOrDefault()?.ProjectedBalance ?? currentBalance;
        var safeToSpend = Math.Max(projectedEnd - buffer, 0);

        var warnings = new List<string>();
        if (negativeBalanceDates.Count > 0)
        {
            warnings.Add($"Negative balance likely around {negativeBalanceDates[0]:dd MMM}.");
        }
        if (projectedEnd < 0)
        {
            warnings.Add("Projected end-of-month balance is below zero.");
        }
        if (safeToSpend <= 0)
        {
            warnings.Add("Spending capacity is tight this month.");
        }

        return new ForecastComputation
        {
            CurrentBalance = currentBalance,
            ProjectedEndBalance = Math.Round(projectedEnd, 2),
            SafeToSpend = Math.Round(safeToSpend, 2),
            UpcomingItems = upcomingItems,
            DailyPoints = dailyPoints,
            RiskWarnings = warnings
        };
    }

    private static Dictionary<DateOnly, decimal> BuildRecurringNetByDay(
        IReadOnlyCollection<RecurringTransaction> recurring,
        IReadOnlySet<Guid> accountIds,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var map = new Dictionary<DateOnly, decimal>();

        foreach (var item in recurring)
        {
            var runDate = item.NextRunDate < fromDate ? fromDate : item.NextRunDate;
            while (runDate <= toDate && (item.EndDate == null || runDate <= item.EndDate.Value))
            {
                if (!map.ContainsKey(runDate))
                {
                    map[runDate] = 0;
                }

                map[runDate] += CalculateNetEffect(item, accountIds);
                runDate = GetNextRun(runDate, item.Frequency);
            }
        }

        return map;
    }

    private static IReadOnlyCollection<ForecastUpcomingItem> BuildUpcomingItems(
        IReadOnlyCollection<RecurringTransaction> recurring,
        IReadOnlySet<Guid> accountIds,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var items = new List<ForecastUpcomingItem>();

        foreach (var item in recurring)
        {
            var runDate = item.NextRunDate < fromDate ? fromDate : item.NextRunDate;
            while (runDate <= toDate && (item.EndDate == null || runDate <= item.EndDate.Value))
            {
                items.Add(new ForecastUpcomingItem
                {
                    Date = runDate,
                    Title = item.Title,
                    Amount = item.Amount,
                    Type = DescribeEffect(item, accountIds)
                });
                runDate = GetNextRun(runDate, item.Frequency);
            }
        }

        return items
            .OrderBy(x => x.Date)
            .Take(20)
            .ToArray();
    }

    private static decimal CalculateNetEffect(Transaction item, IReadOnlySet<Guid> accountIds)
    {
        return item.Type switch
        {
            TransactionType.Income when accountIds.Contains(item.AccountId) => item.Amount,
            TransactionType.Expense when accountIds.Contains(item.AccountId) => -item.Amount,
            TransactionType.Transfer when accountIds.Contains(item.AccountId) &&
                                            item.DestinationAccountId.HasValue &&
                                            accountIds.Contains(item.DestinationAccountId.Value) => 0,
            TransactionType.Transfer when accountIds.Contains(item.AccountId) => -item.Amount,
            TransactionType.Transfer when item.DestinationAccountId.HasValue &&
                                            accountIds.Contains(item.DestinationAccountId.Value) => item.Amount,
            _ => 0
        };
    }

    private static decimal CalculateNetEffect(RecurringTransaction item, IReadOnlySet<Guid> accountIds)
    {
        return item.Type switch
        {
            TransactionType.Income when accountIds.Contains(item.AccountId) => item.Amount,
            TransactionType.Expense when accountIds.Contains(item.AccountId) => -item.Amount,
            TransactionType.Transfer when accountIds.Contains(item.AccountId) &&
                                            item.DestinationAccountId.HasValue &&
                                            accountIds.Contains(item.DestinationAccountId.Value) => 0,
            TransactionType.Transfer when accountIds.Contains(item.AccountId) => -item.Amount,
            TransactionType.Transfer when item.DestinationAccountId.HasValue &&
                                            accountIds.Contains(item.DestinationAccountId.Value) => item.Amount,
            _ => 0
        };
    }

    private static string DescribeEffect(RecurringTransaction item, IReadOnlySet<Guid> accountIds)
    {
        var effect = CalculateNetEffect(item, accountIds);
        if (effect > 0)
        {
            return "Expected income";
        }

        if (effect < 0)
        {
            return "Expected expense";
        }

        return "Internal transfer";
    }

    private static DateOnly GetNextRun(DateOnly date, RecurringFrequency frequency) =>
        frequency switch
        {
            RecurringFrequency.Daily => date.AddDays(1),
            RecurringFrequency.Weekly => date.AddDays(7),
            RecurringFrequency.Monthly => date.AddMonths(1),
            RecurringFrequency.Yearly => date.AddYears(1),
            _ => date.AddMonths(1)
        };

    private sealed class ForecastComputation
    {
        public required decimal CurrentBalance { get; init; }
        public required decimal ProjectedEndBalance { get; init; }
        public required decimal SafeToSpend { get; init; }
        public required IReadOnlyCollection<ForecastUpcomingItem> UpcomingItems { get; init; }
        public required IReadOnlyCollection<ForecastDailyPoint> DailyPoints { get; init; }
        public required IReadOnlyCollection<string> RiskWarnings { get; init; }
    }
}
