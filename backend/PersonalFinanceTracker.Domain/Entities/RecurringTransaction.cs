using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class RecurringTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public Account? DestinationAccount { get; set; }
    public RecurringFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextRunDate { get; set; }
    public bool AutoCreateTransaction { get; set; } = true;
    public bool IsPaused { get; set; }

    public ICollection<Transaction> GeneratedTransactions { get; set; } = new List<Transaction>();
}
