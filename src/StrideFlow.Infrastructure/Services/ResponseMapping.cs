using StrideFlow.Application.Models.Sessions;
using StrideFlow.Application.Models.Users;
using StrideFlow.Domain.Tracking;
using StrideFlow.Domain.Users;

namespace StrideFlow.Infrastructure.Services;

internal static class ResponseMapping
{
    public static UserProfileResponse ToResponse(this User user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.Username,
            user.DisplayName,
            user.Bio,
            user.City,
            user.TimeZoneId,
            user.AccentColor,
            user.HeightCm,
            user.WeightKg,
            user.StepLengthMeters,
            user.DailyStepGoal,
            user.IsProfilePublic,
            user.CreatedAt,
            user.LastSeenAt);
    }

    public static WalkingSessionResponse ToResponse(this WalkingSession session)
    {
        return new WalkingSessionResponse(
            session.Id,
            session.Name,
            session.Status.ToString().ToLowerInvariant(),
            session.StartedAt,
            session.LastPointRecordedAt,
            session.CompletedAt,
            session.TotalDistanceMeters,
            session.TotalSteps,
            session.CaloriesBurned,
            session.DurationSeconds);
    }

    public static WalkingSessionPointResponse ToResponse(this WalkingSessionPoint point)
    {
        return new WalkingSessionPointResponse(
            point.Latitude,
            point.Longitude,
            point.AccuracyMeters,
            point.DistanceFromPreviousMeters,
            point.StepDelta,
            point.SpeedMetersPerSecond,
            point.RecordedAt);
    }

    public static WalkingSessionDetailResponse ToDetailResponse(this WalkingSession session, IReadOnlyCollection<WalkingSessionPoint> points)
    {
        var orderedPoints = points.OrderBy(x => x.Sequence).ToList();
        var averageSpeed = session.DurationSeconds > 0
            ? Math.Round(session.TotalDistanceMeters / session.DurationSeconds, 2, MidpointRounding.AwayFromZero)
            : 0d;
        var averagePace = session.TotalDistanceMeters > 0
            ? Math.Round(session.DurationSeconds / (session.TotalDistanceMeters / 1_000d), 2, MidpointRounding.AwayFromZero)
            : 0d;

        return new WalkingSessionDetailResponse(
            session.ToResponse(),
            orderedPoints.Select(ToResponse).ToList(),
            averagePace,
            averageSpeed);
    }

    public static LiveSessionResponse ToResponse(this LiveSessionSnapshot snapshot)
    {
        return new LiveSessionResponse(
            snapshot.SessionId,
            snapshot.UserId,
            snapshot.DisplayName,
            snapshot.AccentColor,
            snapshot.SessionName,
            snapshot.Status,
            snapshot.Latitude,
            snapshot.Longitude,
            snapshot.TotalDistanceMeters,
            snapshot.TotalSteps,
            snapshot.CaloriesBurned,
            snapshot.UpdatedAt,
            snapshot.TailPoints);
    }
}
