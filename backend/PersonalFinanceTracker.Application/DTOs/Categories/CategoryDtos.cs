using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Application.DTOs.Categories;

public sealed class CategoryResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required CategoryType Type { get; init; }
    public required string Color { get; init; }
    public required string Icon { get; init; }
    public required bool IsArchived { get; init; }
}

public sealed class CreateCategoryRequest
{
    public required string Name { get; init; }
    public required CategoryType Type { get; init; }
    public string Color { get; init; } = "#3b82f6";
    public string Icon { get; init; } = "wallet";
}

public sealed class UpdateCategoryRequest
{
    public required string Name { get; init; }
    public required string Color { get; init; }
    public required string Icon { get; init; }
    public bool IsArchived { get; init; }
}
