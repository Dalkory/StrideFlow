using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Common;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Auth;
using StrideFlow.Application.Models.Users;
using StrideFlow.Domain.Auth;
using StrideFlow.Domain.Users;
using StrideFlow.Infrastructure.Database;

namespace StrideFlow.Infrastructure.Services;

public class AuthService(
    StrideFlowDbContext dbContext,
    IAuthTokenService authTokenService,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, ClientContext clientContext, CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);
        var username = Normalize(request.Username);

        var exists = await dbContext.Users
            .AnyAsync(x => x.Email == email || x.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new AppException(409, "A user with the same email or username already exists.", "user_exists");
        }

        var now = timeProvider.GetUtcNow();
        var user = User.Create(
            Guid.NewGuid(),
            email,
            username,
            request.DisplayName.Trim(),
            string.Empty,
            string.Empty,
            request.City.Trim(),
            request.TimeZoneId.Trim(),
            GenerateAccentColor(username),
            request.HeightCm,
            request.WeightKg,
            GetDefaultStepLength(request.HeightCm),
            request.DailyStepGoal,
            true,
            now);

        user.UpdatePasswordHash(passwordHasher.HashPassword(user, request.Password), now);

        var refreshTokenValue = authTokenService.CreateRefreshToken();
        var refreshToken = BuildRefreshToken(user.Id, refreshTokenValue, clientContext, now, null);

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BuildAuthResponse(user, refreshTokenValue);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, ClientContext clientContext, CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new AppException(401, "Invalid email or password.", "invalid_credentials");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new AppException(401, "Invalid email or password.", "invalid_credentials");
        }

        var now = timeProvider.GetUtcNow();
        user.Touch(now);

        var refreshTokenValue = authTokenService.CreateRefreshToken();
        var refreshToken = BuildRefreshToken(user.Id, refreshTokenValue, clientContext with { DeviceName = request.DeviceName ?? clientContext.DeviceName }, now, null);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BuildAuthResponse(user, refreshTokenValue);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, ClientContext clientContext, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = authTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            throw new AppException(401, "Refresh token is invalid.", "invalid_refresh_token");
        }

        if (!storedToken.IsActive(now))
        {
            await RevokeTokenFamilyAsync(storedToken.TokenFamilyId, clientContext.IpAddress, cancellationToken).ConfigureAwait(false);
            throw new AppException(401, "Refresh token is no longer active.", "inactive_refresh_token");
        }

        storedToken.Revoke(now, clientContext.IpAddress, "Rotated");

        var newRefreshTokenValue = authTokenService.CreateRefreshToken();
        var replacement = BuildRefreshToken(storedToken.UserId, newRefreshTokenValue, clientContext, now, storedToken.TokenFamilyId);

        storedToken.MarkReplacedBy(replacement.Id);
        dbContext.RefreshTokens.Add(replacement);
        storedToken.User.Touch(now);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BuildAuthResponse(storedToken.User, newRefreshTokenValue);
    }

    public async Task LogoutAsync(Guid userId, string? jwtId, LogoutRequest request, ClientContext clientContext, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = authTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            return;
        }

        if (storedToken.RevokedAt is null)
        {
            storedToken.Revoke(now, clientContext.IpAddress, $"Logout:{jwtId ?? "unknown"}");
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new AppException(404, "User not found.", "user_not_found");
        }

        return user.ToResponse();
    }

    private async Task RevokeTokenFamilyAsync(Guid tokenFamilyId, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokens = await dbContext.RefreshTokens
            .Where(x => x.TokenFamilyId == tokenFamilyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(now, ipAddress, "Token family compromised");
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private AuthResponse BuildAuthResponse(User user, string refreshTokenValue)
    {
        var (accessToken, expiresAt, _) = authTokenService.CreateAccessToken(user);

        return new AuthResponse(
            new AuthTokensResponse(accessToken, refreshTokenValue, expiresAt),
            user.ToResponse());
    }

    private RefreshToken BuildRefreshToken(Guid userId, string refreshTokenValue, ClientContext clientContext, DateTimeOffset now, Guid? tokenFamilyId)
    {
        var expiresAt = now.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays);

        return RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            tokenFamilyId ?? Guid.NewGuid(),
            authTokenService.HashRefreshToken(refreshTokenValue),
            clientContext.DeviceName,
            clientContext.IpAddress,
            clientContext.UserAgent,
            now,
            expiresAt);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static double GetDefaultStepLength(double heightCm)
    {
        return Math.Round((heightCm * 0.415d) / 100d, 2, MidpointRounding.AwayFromZero);
    }

    private static string GenerateAccentColor(string seed)
    {
        var hash = seed.Aggregate(0, (current, character) => current + character);
        var colors = new[]
        {
            "#2a9d8f",
            "#f4a261",
            "#e76f51",
            "#457b9d",
            "#8d99ae",
            "#588157"
        };

        return colors[Math.Abs(hash) % colors.Length];
    }
}
