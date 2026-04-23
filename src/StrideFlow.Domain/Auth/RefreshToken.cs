using StrideFlow.Domain.Users;

namespace StrideFlow.Domain.Auth;

public class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid TokenFamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string? DeviceName { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? CreatedByUserAgent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? RevocationReason { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public RefreshToken? ReplacedByToken { get; private set; }

    public User User { get; private set; } = default!;

    public static RefreshToken Create(
        Guid id,
        Guid userId,
        Guid tokenFamilyId,
        string tokenHash,
        string? deviceName,
        string? createdByIp,
        string? createdByUserAgent,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenFamilyId = tokenFamilyId,
            TokenHash = tokenHash,
            DeviceName = deviceName,
            CreatedByIp = createdByIp,
            CreatedByUserAgent = createdByUserAgent,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && !IsExpired(now);

    public void Revoke(DateTimeOffset revokedAt, string? revokedByIp, string reason)
    {
        RevokedAt = revokedAt;
        RevokedByIp = revokedByIp;
        RevocationReason = reason;
    }

    public void MarkReplacedBy(Guid newTokenId)
    {
        ReplacedByTokenId = newTokenId;
    }
}
