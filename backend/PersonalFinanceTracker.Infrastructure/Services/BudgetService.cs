using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Budgets;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class BudgetService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IBudgetService
{
    public async Task<IReadOnlyCollection<BudgetResponse>> GetAllAsync(int month, int year, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var budgets = await dbContext.Budgets
            .Include(x => x.Category)
            .Where(x => x.UserId == userId && x.Month == month && x.Year == year)
            .OrderBy(x => x.Category!.Name)
            .ToListAsync(cancellationToken);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var actuals = await dbContext.Transactions
            .Where(x => x.UserId == userId &&
                        x.Type == TransactionType.Expense &&
                        x.CategoryId != null &&
                        x.TransactionDate >= startDate &&
                        x.TransactionDate <= endDate)
            .GroupBy(x => x.CategoryId!.Value)
            .Select(group => new { CategoryId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Amount, cancellationToken);

        return budgets.Select(budget =>
        {
            var actual = actuals.GetValueOrDefault(budget.CategoryId);
            return new BudgetResponse
            {
                Id = budget.Id,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? string.Empty,
                CategoryColor = budget.Category?.Color ?? "#3b82f6",
                Month = budget.Month,
                Year = budget.Year,
                Amount = budget.Amount,
                ActualSpend = actual,
                UtilizationPercent = budget.Amount == 0 ? 0 : Math.Round((actual / budget.Amount) * 100, 2),
                AlertThresholdPercent = budget.AlertThresholdPercent
            };
        }).ToArray();
    }

    public async Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstNonPositiveAmount(request.Amount);
        ValidateBudgetDate(request.Month, request.Year);

        var category = await dbContext.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.UserId == userId, cancellationToken)
            ?? throw new ValidationException("Category does not exist.");

        var exists = await dbContext.Budgets.AnyAsync(
            x => x.UserId == userId && x.CategoryId == request.CategoryId && x.Month == request.Month && x.Year == request.Year,
            cancellationToken);

        if (exists)
        {
            throw new ValidationException("Budget already exists for this category and month.");
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Month = request.Month,
            Year = request.Year,
            Amount = request.Amount,
            AlertThresholdPercent = request.AlertThresholdPercent
        };

        await dbContext.Budgets.AddAsync(budget, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "budget_created", nameof(Budget), budget.Id, new { category.Name, budget.Amount }, cancellationToken);

        return (await GetAllAsync(request.Month, request.Year, cancellationToken)).Single(x => x.Id == budget.Id);
    }

    public async Task<BudgetResponse> UpdateAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstNonPositiveAmount(request.Amount);

        var budget = await dbContext.Budgets.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Budget not found.");

        budget.Amount = request.Amount;
        budget.AlertThresholdPercent = request.AlertThresholdPercent;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "budget_updated", nameof(Budget), budget.Id, new { budget.Amount }, cancellationToken);

        return (await GetAllAsync(budget.Month, budget.Year, cancellationToken)).Single(x => x.Id == budget.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var budget = await dbContext.Budgets.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Budget not found.");

        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "budget_deleted", nameof(Budget), budget.Id, new { budget.Month, budget.Year }, cancellationToken);
    }

    private static void ValidateBudgetDate(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            throw new ValidationException("Month must be between 1 and 12.");
        }

        if (year is < 2000 or > 2100)
        {
            throw new ValidationException("Year is invalid.");
        }
    }
}
