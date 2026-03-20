using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Transactions;

public sealed class TransactionResponse
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public string? DestinationAccountName { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public string? Note { get; init; }
    public string? Merchant { get; init; }
    public string? PaymentMethod { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public Guid? RecurringTransactionId { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed class TransactionQueryRequest
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? AccountId { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public TransactionType? Type { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CreateTransactionRequest
{
    public required Guid AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public string? Note { get; init; }
    public string? Merchant { get; init; }
    public string? PaymentMethod { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public Guid? RecurringTransactionId { get; init; }
}

public sealed class UpdateTransactionRequest
{
    public required Guid AccountId { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public string? Note { get; init; }
    public string? Merchant { get; init; }
    public string? PaymentMethod { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}
