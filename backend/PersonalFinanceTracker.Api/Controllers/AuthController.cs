using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Auth;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public Task<AuthResponse> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        authService.RegisterAsync(request, cancellationToken);

    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, cancellationToken);

    [HttpPost("forgot-password")]
    public Task<ForgotPasswordResponse> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        authService.ForgotPasswordAsync(request, cancellationToken);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
