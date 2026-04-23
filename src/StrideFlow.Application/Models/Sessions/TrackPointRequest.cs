namespace StrideFlow.Application.Models.Sessions;

public sealed record TrackPointRequest(
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    double? AltitudeMeters,
    DateTimeOffset RecordedAt);
