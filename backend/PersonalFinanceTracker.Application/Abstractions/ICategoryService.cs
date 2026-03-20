using PersonalFinanceTracker.Application.DTOs.Categories;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
}
