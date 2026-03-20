namespace PersonalFinanceTracker.Application.DTOs.Auth;

public sealed class ForgotPasswordResponse
{
    public required string Message { get; init; }
    public string? ResetToken { get; init; }
}
