using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Accounts;

public sealed class AccountMemberResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required AccountMemberRole Role { get; init; }
    public required bool IsOwner { get; init; }
}

public sealed class InviteAccountMemberRequest
{
    public required string Email { get; init; }
    public AccountMemberRole Role { get; init; } = AccountMemberRole.Editor;
}

public sealed class UpdateAccountMemberRoleRequest
{
    public required AccountMemberRole Role { get; init; }
}
