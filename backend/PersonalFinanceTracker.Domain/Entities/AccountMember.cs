using PersonalFinanceTracker.Domain.Common;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Domain.Entities;

public sealed class AccountMember : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public AccountMemberRole Role { get; set; } = AccountMemberRole.Viewer;
}
