namespace StrideFlow.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();

    string? GetCurrentJwtId();
}
