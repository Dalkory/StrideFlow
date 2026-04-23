using StrideFlow.Domain.Users;

namespace StrideFlow.Application.Abstractions.Auth;

public interface IAuthTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt, string JwtId) CreateAccessToken(User user);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
