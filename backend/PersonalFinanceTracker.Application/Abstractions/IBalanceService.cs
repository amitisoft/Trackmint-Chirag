namespace PersonalFinanceTracker.Application.Abstractions;

public interface IBalanceService
{
    Task RecalculateForUserAsync(Guid userId, CancellationToken cancellationToken);
}
