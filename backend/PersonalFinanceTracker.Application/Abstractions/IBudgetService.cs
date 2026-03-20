using PersonalFinanceTracker.Application.DTOs.Budgets;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IBudgetService
{
    Task<IReadOnlyCollection<BudgetResponse>> GetAllAsync(int month, int year, CancellationToken cancellationToken);
    Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken);
    Task<BudgetResponse> UpdateAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
