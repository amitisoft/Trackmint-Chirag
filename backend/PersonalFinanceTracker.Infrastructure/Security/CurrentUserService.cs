using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.Exceptions;

namespace PersonalFinanceTracker.Infrastructure.Security;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetUserId()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return userId;
    }

    public string GetEmail()
    {
        var email = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedException("User email is unavailable.");
        }

        return email;
    }
}
