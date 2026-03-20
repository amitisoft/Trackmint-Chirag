using PersonalFinanceTracker.Application.DTOs.Goals;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IGoalService
{
    Task<IReadOnlyCollection<GoalResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<GoalResponse> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken);
    Task<GoalResponse> UpdateAsync(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken);
    Task<GoalResponse> ContributeAsync(Guid id, GoalContributionRequest request, CancellationToken cancellationToken);
    Task<GoalResponse> WithdrawAsync(Guid id, GoalContributionRequest request, CancellationToken cancellationToken);
}
