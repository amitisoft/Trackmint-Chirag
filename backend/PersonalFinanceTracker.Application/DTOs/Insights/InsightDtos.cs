namespace PersonalFinanceTracker.Application.DTOs.Insights;

public sealed class FinancialHealthScoreResponse
{
    public required decimal Score { get; init; }
    public required IReadOnlyCollection<HealthFactorBreakdown> Factors { get; init; }
    public required IReadOnlyCollection<string> Suggestions { get; init; }
}

public sealed class HealthFactorBreakdown
{
    public required string Name { get; init; }
    public required decimal Score { get; init; }
    public required decimal Weight { get; init; }
}

public sealed class InsightCardResponse
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Tone { get; init; }
}
