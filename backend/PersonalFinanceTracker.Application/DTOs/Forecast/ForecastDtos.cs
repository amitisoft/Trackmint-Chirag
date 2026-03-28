namespace PersonalFinanceTracker.Application.DTOs.Forecast;

public sealed class ForecastMonthResponse
{
    public required decimal CurrentBalance { get; init; }
    public required decimal ProjectedEndOfMonthBalance { get; init; }
    public required decimal SafeToSpend { get; init; }
    public required IReadOnlyCollection<ForecastUpcomingItem> UpcomingKnownTransactions { get; init; }
    public required IReadOnlyCollection<string> RiskWarnings { get; init; }
}

public sealed class ForecastUpcomingItem
{
    public required DateOnly Date { get; init; }
    public required string Title { get; init; }
    public required decimal Amount { get; init; }
    public required string Type { get; init; }
}

public sealed class ForecastDailyPoint
{
    public required DateOnly Date { get; init; }
    public required decimal ProjectedBalance { get; init; }
}
