using PersonalFinanceTracker.Application.DTOs.Accounts;
using PersonalFinanceTracker.Application.DTOs.Categories;
using PersonalFinanceTracker.Application.DTOs.Goals;
using PersonalFinanceTracker.Application.DTOs.Recurring;
using PersonalFinanceTracker.Application.DTOs.Transactions;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Infrastructure.Services;

internal static class EntityMapper
{
    public static AccountResponse ToResponse(this Account account) =>
        new()
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            OpeningBalance = account.OpeningBalance,
            CurrentBalance = account.CurrentBalance,
            InstitutionName = account.InstitutionName,
            UpdatedAt = account.UpdatedAt
        };

    public static CategoryResponse ToResponse(this Category category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Color = category.Color,
            Icon = category.Icon,
            IsArchived = category.IsArchived
        };

    public static GoalResponse ToResponse(this Goal goal) =>
        new()
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            ProgressPercent = goal.TargetAmount == 0 ? 0 : Math.Round((goal.CurrentAmount / goal.TargetAmount) * 100, 2),
            TargetDate = goal.TargetDate,
            LinkedAccountId = goal.LinkedAccountId,
            Icon = goal.Icon,
            Color = goal.Color,
            Status = goal.Status
        };

    public static RecurringTransactionResponse ToResponse(this RecurringTransaction item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Type = item.Type,
            Amount = item.Amount,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            AccountId = item.AccountId,
            AccountName = item.Account?.Name,
            DestinationAccountId = item.DestinationAccountId,
            DestinationAccountName = item.DestinationAccount?.Name,
            Frequency = item.Frequency,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            NextRunDate = item.NextRunDate,
            AutoCreateTransaction = item.AutoCreateTransaction,
            IsPaused = item.IsPaused
        };

    public static TransactionResponse ToResponse(this Transaction item) =>
        new()
        {
            Id = item.Id,
            AccountId = item.AccountId,
            AccountName = item.Account?.Name ?? string.Empty,
            DestinationAccountId = item.DestinationAccountId,
            DestinationAccountName = item.DestinationAccount?.Name,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            Type = item.Type,
            Amount = item.Amount,
            Date = item.TransactionDate,
            Note = item.Note,
            Merchant = item.Merchant,
            PaymentMethod = item.PaymentMethod,
            Tags = item.Tags,
            RecurringTransactionId = item.RecurringTransactionId,
            CreatedAt = item.CreatedAt
        };
}
