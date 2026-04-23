using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Common;

namespace StrideFlow.Api.Infrastructure;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetRequiredUserId()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new AppException(401, "The current request is not authenticated.", "unauthorized");
        }

        return userId;
    }

    public string? GetCurrentJwtId()
    {
        return httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
    }
}
