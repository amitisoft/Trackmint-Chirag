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
    ICurrentUserService currentUserService) : IReportService
{
    public async Task<IReadOnlyCollection<CategorySpendReportItem>> GetCategorySpendAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var query = ApplyFilters(dbContext.Transactions.Include(x => x.Category).Where(x => x.UserId == userId), request)
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
        var transactions = await ApplyFilters(dbContext.Transactions.Where(x => x.UserId == userId), request)
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
        var userId = currentUserService.GetUserId();
        var accounts = await dbContext.Accounts
            .Where(x => x.UserId == userId && (!request.AccountId.HasValue || x.Id == request.AccountId.Value))
            .ToListAsync(cancellationToken);

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-5);
        var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var transactions = await ApplyFilters(dbContext.Transactions.Where(x => x.UserId == userId), request)
            .ToListAsync(cancellationToken);

        var months = BuildMonthSeries(startDate, endDate);
        var results = new List<AccountBalanceTrendItem>();

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

            results.Add(new AccountBalanceTrendItem
            {
                Label = month.ToString("MMM yy"),
                Balance = balance
            });
        }

        return results;
    }

    public async Task<ExportFileResponse> ExportTransactionsCsvAsync(ReportFilterRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var transactions = await ApplyFilters(dbContext.Transactions
                .Include(x => x.Account)
                .Include(x => x.DestinationAccount)
                .Include(x => x.Category)
                .Where(x => x.UserId == userId), request)
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
