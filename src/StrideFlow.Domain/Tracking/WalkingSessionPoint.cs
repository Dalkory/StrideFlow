namespace StrideFlow.Domain.Tracking;

public class WalkingSessionPoint
{
    private WalkingSessionPoint()
    {
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public int Sequence { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public double AccuracyMeters { get; private set; }

    public double? AltitudeMeters { get; private set; }

    public double DistanceFromPreviousMeters { get; private set; }

    public int StepDelta { get; private set; }

    public double SpeedMetersPerSecond { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public WalkingSession Session { get; private set; } = default!;

    public static WalkingSessionPoint Create(
        Guid id,
        Guid sessionId,
        int sequence,
        double latitude,
        double longitude,
        double accuracyMeters,
        double? altitudeMeters,
        double distanceFromPreviousMeters,
        int stepDelta,
        double speedMetersPerSecond,
        DateTimeOffset recordedAt)
    {
        return new WalkingSessionPoint
        {
            Id = id,
            SessionId = sessionId,
            Sequence = sequence,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            AltitudeMeters = altitudeMeters,
            DistanceFromPreviousMeters = distanceFromPreviousMeters,
            StepDelta = stepDelta,
            SpeedMetersPerSecond = speedMetersPerSecond,
            RecordedAt = recordedAt
        };
    }
}
