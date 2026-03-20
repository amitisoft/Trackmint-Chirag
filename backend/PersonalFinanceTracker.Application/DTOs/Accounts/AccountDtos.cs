using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Accounts;

public sealed class AccountResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required decimal OpeningBalance { get; init; }
    public required decimal CurrentBalance { get; init; }
    public string? InstitutionName { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public sealed class CreateAccountRequest
{
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required decimal OpeningBalance { get; init; }
    public string? InstitutionName { get; init; }
}

public sealed class UpdateAccountRequest
{
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required decimal OpeningBalance { get; init; }
    public string? InstitutionName { get; init; }
}

public sealed class TransferFundsRequest
{
    public required Guid FromAccountId { get; init; }
    public required Guid ToAccountId { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public string? Note { get; init; }
}
