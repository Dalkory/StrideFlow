using StrideFlow.Application.Common;
using StrideFlow.Application.Models.Auth;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, ClientContext clientContext, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, ClientContext clientContext, CancellationToken cancellationToken);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, ClientContext clientContext, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, string? jwtId, LogoutRequest request, ClientContext clientContext, CancellationToken cancellationToken);

    Task<UserProfileResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
