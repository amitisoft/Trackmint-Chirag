using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Reports;
using PersonalFinanceTracker.Application.DTOs.Transactions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService) : IReportService
{
    public async Task<IReadOnlyCollection<CategorySpendReportItem>> GetCategorySpendAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var query = ApplyFilters(dbContext.Transactions
                .Include(x => x.Category)
                .Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))), request)
            .Where(x => x.Type == TransactionType.Expense && x.Category != null);

        return await query
            .GroupBy(x => new { x.Category!.Name, x.Category.Color })
            .Select(group => new CategorySpendReportItem
            {
                Category = group.Key.Name,
                Amount = group.Sum(x => x.Amount),
                Color = group.Key.Color
            })
            .OrderByDescending(x => x.Amount)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IncomeExpenseTrendItem>> GetIncomeExpenseTrendAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var transactions = await ApplyFilters(
                dbContext.Transactions.Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))),
                request)
            .ToListAsync(cancellationToken);

        return transactions
            .GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(group => new IncomeExpenseTrendItem
            {
                Label = new DateOnly(group.Key.Year, group.Key.Month, 1).ToString("MMM yy"),
                Income = group.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                Expense = group.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AccountBalanceTrendItem>> GetAccountBalanceTrendAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var points = await GetNetWorthAsync(request, cancellationToken);
        return points.Select(x => new AccountBalanceTrendItem
        {
            Label = x.Label,
            Balance = x.NetWorth
        }).ToArray();
    }

    public async Task<ReportTrendsResponse> GetTrendsAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-5);
        var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var transactions = await ApplyFilters(
                dbContext.Transactions
                    .Include(x => x.Category)
                    .Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))),
                request)
            .Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate)
            .ToListAsync(cancellationToken);

        var incomeExpense = transactions
            .GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(group => new IncomeExpenseTrendItem
            {
                Label = new DateOnly(group.Key.Year, group.Key.Month, 1).ToString("MMM yy"),
                Income = group.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                Expense = group.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .ToArray();

        var savingsRate = incomeExpense.Select(x =>
        {
            var rate = x.Income <= 0 ? 0 : ((x.Income - x.Expense) / x.Income) * 100;
            return new SavingsRateTrendItem
            {
                Label = x.Label,
                SavingsRatePercent = Math.Round(rate, 2)
            };
        }).ToArray();

        var categoryTrends = transactions
            .Where(x => x.Type == TransactionType.Expense && x.Category != null)
            .GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month, Category = x.Category!.Name })
            .Select(group => new CategoryTrendItem
            {
                Label = new DateOnly(group.Key.Year, group.Key.Month, 1).ToString("MMM yy"),
                Category = group.Key.Category,
                Amount = group.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Label)
            .ThenByDescending(x => x.Amount)
            .ToArray();

        return new ReportTrendsResponse
        {
            IncomeExpense = incomeExpense,
            SavingsRate = savingsRate,
            CategoryTrends = categoryTrends
        };
    }

    public async Task<IReadOnlyCollection<NetWorthPoint>> GetNetWorthAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);

        var accounts = await dbContext.Accounts
            .Where(x => accountIds.Contains(x.Id) && (!request.AccountId.HasValue || x.Id == request.AccountId.Value))
            .ToListAsync(cancellationToken);

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-5);
        var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var accountIdSet = accounts.Select(x => x.Id).ToHashSet();
        var transactions = await ApplyFilters(
                dbContext.Transactions.Where(x => accountIdSet.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIdSet.Contains(x.DestinationAccountId.Value))),
                request)
            .ToListAsync(cancellationToken);

        var months = BuildMonthSeries(startDate, endDate);
        var results = new List<NetWorthPoint>();

        foreach (var month in months)
        {
            var monthEnd = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
            decimal balance = accounts.Sum(x => x.OpeningBalance);

            foreach (var transaction in transactions.Where(x => x.TransactionDate <= monthEnd))
            {
                balance += transaction.Type switch
                {
                    TransactionType.Income => transaction.Amount,
                    TransactionType.Expense => -transaction.Amount,
                    TransactionType.Transfer when !request.AccountId.HasValue => 0,
                    TransactionType.Transfer when request.AccountId.HasValue && transaction.AccountId == request.AccountId.Value => -transaction.Amount,
                    TransactionType.Transfer when request.AccountId.HasValue && transaction.DestinationAccountId == request.AccountId.Value => transaction.Amount,
                    _ => 0
                };
            }

            results.Add(new NetWorthPoint
            {
                Label = month.ToString("MMM yy"),
                NetWorth = balance
            });
        }

        return results;
    }

    public async Task<ExportFileResponse> ExportTransactionsCsvAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var accountIds = await accountAccessService.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var transactions = await ApplyFilters(dbContext.Transactions
                .Include(x => x.Account)
                .Include(x => x.DestinationAccount)
                .Include(x => x.Category)
                .Where(x => accountIds.Contains(x.AccountId) || (x.DestinationAccountId.HasValue && accountIds.Contains(x.DestinationAccountId.Value))), request)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Date,Type,Amount,Account,DestinationAccount,Category,Merchant,Note");

        foreach (var item in transactions)
        {
            builder.AppendLine(string.Join(",",
                Escape($"=\"{item.TransactionDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)}\""),
                Escape(item.Type.ToString()),
                Escape(item.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                Escape(item.Account?.Name ?? string.Empty),
                Escape(item.DestinationAccount?.Name ?? string.Empty),
                Escape(item.Category?.Name ?? string.Empty),
                Escape(item.Merchant ?? string.Empty),
                Escape(item.Note ?? string.Empty)));
        }

        var csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
        var preamble = Encoding.UTF8.GetPreamble();
        var content = new byte[preamble.Length + csvBytes.Length];

        Buffer.BlockCopy(preamble, 0, content, 0, preamble.Length);
        Buffer.BlockCopy(csvBytes, 0, content, preamble.Length, csvBytes.Length);

        return new ExportFileResponse
        {
            FileName = $"trackmint-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            Content = content,
            ContentType = "text/csv"
        };
    }

    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, ReportFilterRequest request)
    {
        return TransactionService.ApplyFilters(query, new TransactionQueryRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Type = request.Type,
            Page = 1,
            PageSize = 5000
        });
    }

    private static IEnumerable<DateOnly> BuildMonthSeries(DateOnly startDate, DateOnly endDate)
    {
        var current = new DateOnly(startDate.Year, startDate.Month, 1);
        var final = new DateOnly(endDate.Year, endDate.Month, 1);

        while (current <= final)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
