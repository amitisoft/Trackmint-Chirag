using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public Account? DestinationAccount { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Note { get; set; }
    public string? Merchant { get; set; }
    public string? PaymentMethod { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Guid? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }
}
