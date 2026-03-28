using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Insights;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class InsightsService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService) : IInsightsService
{
    public async Task<FinancialHealthScoreResponse> GetHealthScoreAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var threeMonthStart = monthStart.AddMonths(-2);

        var transactions = await dbContext.Transactions
            .Where(x =>
                x.TransactionDate >= threeMonthStart &&
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))))
            .ToArrayAsync(cancellationToken);

        var budgets = await dbContext.Budgets
            .Where(x => x.UserId == userId && x.Month == today.Month && x.Year == today.Year)
            .ToArrayAsync(cancellationToken);

        var currentBalance = await dbContext.Accounts
            .Where(x => accountIds.Contains(x.Id))
            .SumAsync(x => x.CurrentBalance, cancellationToken);

        var savingsRate = CalculateSavingsRateScore(transactions, monthStart);
        var expenseStability = CalculateExpenseStabilityScore(transactions, monthStart);
        var budgetAdherence = CalculateBudgetAdherenceScore(transactions, budgets, today);
        var cashBuffer = CalculateCashBufferScore(currentBalance, transactions, monthStart);

        var weighted = (savingsRate * 0.30m) + (expenseStability * 0.20m) + (budgetAdherence * 0.25m) + (cashBuffer * 0.25m);
        var score = Math.Round(weighted, 1);

        var suggestions = new List<string>();
        if (savingsRate < 55) suggestions.Add("Try to save at least 20% of income by trimming low-value expenses.");
        if (expenseStability < 60) suggestions.Add("Expense volatility is high. Add tighter category budgets for variable spends.");
        if (budgetAdherence < 60) suggestions.Add("Review categories breaching budget limits and add spending alerts.");
        if (cashBuffer < 60) suggestions.Add("Build a larger emergency buffer to cover at least one month of expenses.");
        if (suggestions.Count == 0) suggestions.Add("Great discipline. Maintain this consistency and optimize long-term investments.");

        return new FinancialHealthScoreResponse
        {
            Score = score,
            Factors =
            [
                new HealthFactorBreakdown { Name = "Savings Rate", Score = Math.Round(savingsRate, 1), Weight = 0.30m },
                new HealthFactorBreakdown { Name = "Expense Stability", Score = Math.Round(expenseStability, 1), Weight = 0.20m },
                new HealthFactorBreakdown { Name = "Budget Adherence", Score = Math.Round(budgetAdherence, 1), Weight = 0.25m },
                new HealthFactorBreakdown { Name = "Cash Buffer", Score = Math.Round(cashBuffer, 1), Weight = 0.25m }
            ],
            Suggestions = suggestions
        };
    }

    public async Task<IReadOnlyCollection<InsightCardResponse>> GetInsightsAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var previousMonthEnd = currentMonthStart.AddDays(-1);

        var transactions = await dbContext.Transactions
            .Include(x => x.Category)
            .Where(x =>
                x.TransactionDate >= previousMonthStart &&
                (accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))))
            .ToArrayAsync(cancellationToken);

        var currentMonth = transactions.Where(x => x.TransactionDate >= currentMonthStart).ToArray();
        var previousMonth = transactions.Where(x => x.TransactionDate >= previousMonthStart && x.TransactionDate <= previousMonthEnd).ToArray();

        var currentIncome = currentMonth.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var currentExpense = currentMonth.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        var previousIncome = previousMonth.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var previousExpense = previousMonth.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

        var currentSavings = currentIncome - currentExpense;
        var previousSavings = previousIncome - previousExpense;

        var currentFoodExpense = currentMonth
            .Where(x => x.Type == TransactionType.Expense && x.Category != null && x.Category.Name.ToLower().Contains("food"))
            .Sum(x => x.Amount);
        var previousFoodExpense = previousMonth
            .Where(x => x.Type == TransactionType.Expense && x.Category != null && x.Category.Name.ToLower().Contains("food"))
            .Sum(x => x.Amount);

        var insights = new List<InsightCardResponse>
        {
            new()
            {
                Title = "Savings Momentum",
                Message = currentSavings >= previousSavings
                    ? "You saved more than last month. Great momentum."
                    : "Savings dropped compared to last month. Review optional expenses.",
                Tone = currentSavings >= previousSavings ? "positive" : "warning"
            },
            new()
            {
                Title = "Food Spend Trend",
                Message = BuildPercentMessage("Food spending", previousFoodExpense, currentFoodExpense),
                Tone = currentFoodExpense <= previousFoodExpense ? "positive" : "warning"
            },
            new()
            {
                Title = "Expense Trend",
                Message = BuildPercentMessage("Overall expenses", previousExpense, currentExpense),
                Tone = currentExpense <= previousExpense ? "positive" : "warning"
            }
        };

        return insights;
    }

    private static decimal CalculateSavingsRateScore(IReadOnlyCollection<Domain.Entities.Transaction> transactions, DateOnly monthStart)
    {
        var monthTransactions = transactions.Where(x => x.TransactionDate >= monthStart).ToArray();
        var income = monthTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var expense = monthTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
        if (income <= 0)
        {
            return 40;
        }

        var ratio = (income - expense) / income;
        var normalized = Math.Clamp(ratio, 0, 0.4m) / 0.4m;
        return normalized * 100;
    }

    private static decimal CalculateExpenseStabilityScore(IReadOnlyCollection<Domain.Entities.Transaction> transactions, DateOnly monthStart)
    {
        var monthBuckets = Enumerable.Range(0, 3)
            .Select(offset => monthStart.AddMonths(-offset))
            .Select(month => transactions
                .Where(x => x.Type == TransactionType.Expense && x.TransactionDate.Year == month.Year && x.TransactionDate.Month == month.Month)
                .Sum(x => x.Amount))
            .ToArray();

        var average = monthBuckets.Average();
        if (average <= 0)
        {
            return 70;
        }

        var variance = monthBuckets
            .Select(x =>
            {
                var delta = x - average;
                return (double)(delta * delta);
            })
            .Average();

        var stdDev = (decimal)Math.Sqrt(variance);
        var variationRatio = stdDev / average;

        return Math.Clamp(100 - (variationRatio * 180), 0, 100);
    }

    private static decimal CalculateBudgetAdherenceScore(
        IReadOnlyCollection<Domain.Entities.Transaction> transactions,
        IReadOnlyCollection<Domain.Entities.Budget> budgets,
        DateOnly today)
    {
        if (budgets.Count == 0)
        {
            return 65;
        }

        var monthTransactions = transactions.Where(x => x.TransactionDate.Year == today.Year && x.TransactionDate.Month == today.Month).ToArray();
        var onTrackCount = budgets.Count(budget =>
        {
            var spend = monthTransactions
                .Where(x => x.Type == TransactionType.Expense && x.CategoryId == budget.CategoryId)
                .Sum(x => x.Amount);
            return budget.Amount == 0 || spend <= budget.Amount;
        });

        return (onTrackCount / (decimal)budgets.Count) * 100;
    }

    private static decimal CalculateCashBufferScore(
        decimal currentBalance,
        IReadOnlyCollection<Domain.Entities.Transaction> transactions,
        DateOnly monthStart)
    {
        var monthlyExpense = transactions
            .Where(x => x.Type == TransactionType.Expense && x.TransactionDate >= monthStart)
            .Sum(x => x.Amount);

        if (monthlyExpense <= 0)
        {
            return 80;
        }

        var ratio = Math.Abs(currentBalance) / monthlyExpense;
        return Math.Clamp(ratio * 100, 0, 100);
    }

    private static string BuildPercentMessage(string label, decimal previous, decimal current)
    {
        if (previous <= 0 && current <= 0)
        {
            return $"{label} is stable with low baseline activity.";
        }

        if (previous <= 0)
        {
            return $"{label} started this month with new activity.";
        }

        var deltaPercent = ((current - previous) / previous) * 100;
        var direction = deltaPercent >= 0 ? "increased" : "decreased";
        return $"{label} {direction} {Math.Abs(Math.Round(deltaPercent, 1))}% vs last month.";
    }
}
