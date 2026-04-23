using StrideFlow.Domain.Auth;
using StrideFlow.Domain.Tracking;

namespace StrideFlow.Domain.Users;

public class User
{
    private readonly List<RefreshToken> refreshTokens = [];
    private readonly List<WalkingSession> walkingSessions = [];

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Username { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string Bio { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = "UTC";

    public string AccentColor { get; private set; } = "#2a9d8f";

    public double HeightCm { get; private set; }

    public double WeightKg { get; private set; }

    public double StepLengthMeters { get; private set; }

    public int DailyStepGoal { get; private set; }

    public bool IsProfilePublic { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => refreshTokens;

    public IReadOnlyCollection<WalkingSession> WalkingSessions => walkingSessions;

    public static User Create(
        Guid id,
        string email,
        string username,
        string displayName,
        string passwordHash,
        string bio,
        string city,
        string timeZoneId,
        string accentColor,
        double heightCm,
        double weightKg,
        double stepLengthMeters,
        int dailyStepGoal,
        bool isProfilePublic,
        DateTimeOffset now)
    {
        return new User
        {
            Id = id,
            Email = email,
            Username = username,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Bio = bio,
            City = city,
            TimeZoneId = timeZoneId,
            AccentColor = accentColor,
            HeightCm = heightCm,
            WeightKg = weightKg,
            StepLengthMeters = stepLengthMeters,
            DailyStepGoal = dailyStepGoal,
            IsProfilePublic = isProfilePublic,
            CreatedAt = now,
            UpdatedAt = now,
            LastSeenAt = now
        };
    }

    public void UpdateProfile(
        string displayName,
        string bio,
        string city,
        string timeZoneId,
        string accentColor,
        double heightCm,
        double weightKg,
        double stepLengthMeters,
        int dailyStepGoal,
        bool isProfilePublic,
        DateTimeOffset now)
    {
        DisplayName = displayName;
        Bio = bio;
        City = city;
        TimeZoneId = timeZoneId;
        AccentColor = accentColor;
        HeightCm = heightCm;
        WeightKg = weightKg;
        StepLengthMeters = stepLengthMeters;
        DailyStepGoal = dailyStepGoal;
        IsProfilePublic = isProfilePublic;
        UpdatedAt = now;
    }

    public void UpdatePasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAt = now;
    }

    public void Touch(DateTimeOffset now)
    {
        LastSeenAt = now;
        UpdatedAt = now;
    }
}
