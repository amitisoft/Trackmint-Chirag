namespace PersonalFinanceTracker.Application.Abstractions;

public interface IDefaultCategorySeeder
{
    Task SeedAsync(Guid userId, CancellationToken cancellationToken);
}
