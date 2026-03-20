namespace PersonalFinanceTracker.Application.Abstractions;

public interface ICurrentUserService
{
    Guid GetUserId();
    string GetEmail();
}
