using StrideFlow.Application.Models.Users;

namespace StrideFlow.Application.Models.Auth;

public sealed record AuthResponse(
    AuthTokensResponse Tokens,
    UserProfileResponse User);
