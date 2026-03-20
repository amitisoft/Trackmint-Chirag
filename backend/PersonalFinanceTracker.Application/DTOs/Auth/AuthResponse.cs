namespace PersonalFinanceTracker.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}
