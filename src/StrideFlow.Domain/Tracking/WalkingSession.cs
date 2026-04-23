using StrideFlow.Domain.Users;

namespace StrideFlow.Domain.Tracking;

public class WalkingSession
{
    private readonly List<WalkingSessionPoint> points = [];

    private WalkingSession()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public WalkingSessionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? PausedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? LastPointRecordedAt { get; private set; }

    public double TotalDistanceMeters { get; private set; }

    public int TotalSteps { get; private set; }

    public double CaloriesBurned { get; private set; }

    public int DurationSeconds { get; private set; }

    public int PausedDurationSeconds { get; private set; }

    public IReadOnlyCollection<WalkingSessionPoint> Points => points;

    public User User { get; private set; } = default!;

    public static WalkingSession Create(Guid id, Guid userId, string name, DateTimeOffset startedAt)
    {
        return new WalkingSession
        {
            Id = id,
            UserId = userId,
            Name = name,
            Status = WalkingSessionStatus.Active,
            StartedAt = startedAt
        };
    }

    public void AddPoint(WalkingSessionPoint point)
    {
        points.Add(point);
        LastPointRecordedAt = point.RecordedAt;
    }

    public void UpdateMetrics(double totalDistanceMeters, int totalSteps, double caloriesBurned, int durationSeconds)
    {
        TotalDistanceMeters = totalDistanceMeters;
        TotalSteps = totalSteps;
        CaloriesBurned = caloriesBurned;
        DurationSeconds = durationSeconds;
    }

    public void Pause(DateTimeOffset pausedAt)
    {
        if (Status != WalkingSessionStatus.Active)
        {
            return;
        }

        Status = WalkingSessionStatus.Paused;
        PausedAt = pausedAt;
    }

    public void Resume(DateTimeOffset resumedAt)
    {
        if (Status != WalkingSessionStatus.Paused || PausedAt is null)
        {
            return;
        }

        PausedDurationSeconds += Math.Max(0, (int)(resumedAt - PausedAt.Value).TotalSeconds);
        PausedAt = null;
        Status = WalkingSessionStatus.Active;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == WalkingSessionStatus.Completed)
        {
            return;
        }

        if (Status == WalkingSessionStatus.Paused && PausedAt is not null)
        {
            PausedDurationSeconds += Math.Max(0, (int)(completedAt - PausedAt.Value).TotalSeconds);
            PausedAt = null;
        }

        Status = WalkingSessionStatus.Completed;
        CompletedAt = completedAt;
    }
}
