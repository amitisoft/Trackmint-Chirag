using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Recurring;

public sealed class RecurringTransactionResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public required Guid AccountId { get; init; }
    public string? AccountName { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public string? DestinationAccountName { get; init; }
    public required RecurringFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required DateOnly NextRunDate { get; init; }
    public required bool AutoCreateTransaction { get; init; }
    public required bool IsPaused { get; init; }
}

public sealed class CreateRecurringTransactionRequest
{
    public required string Title { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public Guid? CategoryId { get; init; }
    public required Guid AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public required RecurringFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool AutoCreateTransaction { get; init; } = true;
}

public sealed class UpdateRecurringTransactionRequest
{
    public required string Title { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public Guid? CategoryId { get; init; }
    public required Guid AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public required RecurringFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required DateOnly NextRunDate { get; init; }
    public bool AutoCreateTransaction { get; init; } = true;
    public bool IsPaused { get; init; }
}
