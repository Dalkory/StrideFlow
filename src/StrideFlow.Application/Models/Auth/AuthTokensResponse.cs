namespace StrideFlow.Application.Models.Auth;

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
