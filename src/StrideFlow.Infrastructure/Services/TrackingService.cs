using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrideFlow.Application.Abstractions.Realtime;
using StrideFlow.Application.Abstractions.Tracking;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Common;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Sessions;
using StrideFlow.Domain.Tracking;
using StrideFlow.Infrastructure.Database;

namespace StrideFlow.Infrastructure.Services;

public class TrackingService(
    StrideFlowDbContext dbContext,
    ILiveSessionStore liveSessionStore,
    IRealtimeNotifier realtimeNotifier,
    IUserService userService,
    TimeProvider timeProvider,
    IOptions<TrackingOptions> trackingOptions) : ITrackingService
{
    private readonly TrackingOptions options = trackingOptions.Value;

    public async Task<WalkingSessionDetailResponse> StartAsync(Guid userId, StartSessionRequest request, CancellationToken cancellationToken)
    {
        var hasActiveSession = await dbContext.WalkingSessions
            .AnyAsync(x => x.UserId == userId && x.Status != WalkingSessionStatus.Completed, cancellationToken)
            .ConfigureAwait(false);

        if (hasActiveSession)
        {
            throw new AppException(409, "Finish the current session before starting a new one.", "session_already_active");
        }

        var session = WalkingSession.Create(Guid.NewGuid(), userId, request.Name.Trim(), timeProvider.GetUtcNow());
        dbContext.WalkingSessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session.ToDetailResponse([]);
    }

    public async Task<WalkingSessionDetailResponse?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await dbContext.WalkingSessions
            .Include(x => x.Points)
            .Where(x => x.UserId == userId && x.Status != WalkingSessionStatus.Completed)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return session?.ToDetailResponse(session.Points.ToList());
    }

    public async Task<IReadOnlyList<WalkingSessionResponse>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.WalkingSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartedAt)
            .Take(40)
            .Select(x => x.ToResponse())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WalkingSessionDetailResponse> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionWithPointsAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
        return session.ToDetailResponse(session.Points.ToList());
    }

    public async Task<WalkingSessionDetailResponse> AddPointsAsync(Guid userId, Guid sessionId, TrackSessionPointsRequest request, CancellationToken cancellationToken)
    {
        if (request.Points.Count > options.MaxPointsPerBatch)
        {
            throw new AppException(400, $"A maximum of {options.MaxPointsPerBatch} points can be uploaded at once.", "too_many_points");
        }

        var session = await LoadSessionWithPointsAndUserAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
        if (session.Status != WalkingSessionStatus.Active)
        {
            throw new AppException(409, "Only active sessions can accept new points.", "session_not_active");
        }

        var orderedExistingPoints = session.Points.OrderBy(x => x.Sequence).ToList();
        var lastPoint = orderedExistingPoints.LastOrDefault();
        var sequence = lastPoint?.Sequence + 1 ?? 1;
        var totalDistance = session.TotalDistanceMeters;
        var totalSteps = session.TotalSteps;
        var anyAccepted = false;
        var newPoints = new List<WalkingSessionPoint>();

        foreach (var point in request.Points.OrderBy(x => x.RecordedAt))
        {
            if (point.AccuracyMeters > options.MaxAcceptedAccuracyMeters)
            {
                continue;
            }

            var distance = 0d;
            var speed = 0d;

            if (lastPoint is not null)
            {
                distance = GeoMath.HaversineDistanceMeters(lastPoint.Latitude, lastPoint.Longitude, point.Latitude, point.Longitude);
                speed = GeoMath.EstimateSpeedMetersPerSecond(distance, lastPoint.RecordedAt, point.RecordedAt);

                if (distance > options.MinMeaningfulDistanceMeters && speed > options.MaxReasonableSpeedMetersPerSecond)
                {
                    continue;
                }

                if (distance < options.MinMeaningfulDistanceMeters)
                {
                    distance = 0d;
                    speed = 0d;
                }
            }

            totalDistance += distance;
            var recalculatedSteps = GeoMath.EstimateSteps(totalDistance, session.User.StepLengthMeters);
            var stepDelta = Math.Max(0, recalculatedSteps - totalSteps);
            totalSteps = recalculatedSteps;

            var entity = WalkingSessionPoint.Create(
                Guid.NewGuid(),
                session.Id,
                sequence,
                point.Latitude,
                point.Longitude,
                point.AccuracyMeters,
                point.AltitudeMeters,
                distance,
                stepDelta,
                speed,
                point.RecordedAt);

            sequence++;
            dbContext.WalkingSessionPoints.Add(entity);
            session.AddPoint(entity);
            orderedExistingPoints.Add(entity);
            newPoints.Add(entity);
            lastPoint = entity;
            anyAccepted = true;
        }

        var effectiveAt = lastPoint?.RecordedAt ?? timeProvider.GetUtcNow();
        session.UpdateMetrics(
            totalDistance,
            totalSteps,
            GeoMath.EstimateCalories(totalDistance, session.User.WeightKg),
            CalculateDurationSeconds(session, effectiveAt));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var response = session.ToDetailResponse(orderedExistingPoints);
        if (!anyAccepted || orderedExistingPoints.Count == 0)
        {
            return response;
        }

        await liveSessionStore.UpsertAsync(BuildSnapshot(session, orderedExistingPoints), cancellationToken).ConfigureAwait(false);
        await BroadcastTrackingUpdateAsync(userId, response, false, cancellationToken).ConfigureAwait(false);

        return response;
    }

    public async Task<WalkingSessionDetailResponse> PauseAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionWithPointsAndUserAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
        session.Pause(timeProvider.GetUtcNow());
        session.UpdateMetrics(session.TotalDistanceMeters, session.TotalSteps, session.CaloriesBurned, CalculateDurationSeconds(session, timeProvider.GetUtcNow()));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await liveSessionStore.RemoveAsync(session.Id, cancellationToken).ConfigureAwait(false);

        var response = session.ToDetailResponse(session.Points.ToList());
        await BroadcastTrackingUpdateAsync(userId, response, false, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<WalkingSessionDetailResponse> ResumeAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionWithPointsAndUserAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
        session.Resume(timeProvider.GetUtcNow());
        session.UpdateMetrics(session.TotalDistanceMeters, session.TotalSteps, session.CaloriesBurned, CalculateDurationSeconds(session, timeProvider.GetUtcNow()));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var points = session.Points.OrderBy(x => x.Sequence).ToList();
        if (points.Count > 0)
        {
            await liveSessionStore.UpsertAsync(BuildSnapshot(session, points), cancellationToken).ConfigureAwait(false);
        }

        var response = session.ToDetailResponse(points);
        await BroadcastTrackingUpdateAsync(userId, response, false, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<WalkingSessionDetailResponse> StopAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionWithPointsAndUserAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        session.Complete(now);
        session.UpdateMetrics(session.TotalDistanceMeters, session.TotalSteps, session.CaloriesBurned, CalculateDurationSeconds(session, now));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await liveSessionStore.RemoveAsync(session.Id, cancellationToken).ConfigureAwait(false);

        var response = session.ToDetailResponse(session.Points.ToList());
        await BroadcastTrackingUpdateAsync(userId, response, true, cancellationToken).ConfigureAwait(false);

        return response;
    }

    public async Task<IReadOnlyList<LiveSessionResponse>> GetLiveMapAsync(CancellationToken cancellationToken)
    {
        var snapshots = await liveSessionStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return snapshots
            .Where(x => x.Status == WalkingSessionStatus.Active.ToString().ToLowerInvariant())
            .OrderByDescending(x => x.TotalSteps)
            .ThenByDescending(x => x.TotalDistanceMeters)
            .Select(x => x.ToResponse())
            .ToList();
    }

    private async Task BroadcastTrackingUpdateAsync(Guid userId, WalkingSessionDetailResponse response, bool completed, CancellationToken cancellationToken)
    {
        var liveMap = await GetLiveMapAsync(cancellationToken).ConfigureAwait(false);
        var leaderboard = await userService.GetLeaderboardAsync(userId, "day", 10, null, cancellationToken).ConfigureAwait(false);

        if (completed)
        {
            await realtimeNotifier.BroadcastSessionCompletedAsync(response, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await realtimeNotifier.BroadcastSessionUpdatedAsync(response, cancellationToken).ConfigureAwait(false);
        }

        await realtimeNotifier.BroadcastLiveMapUpdatedAsync(liveMap, cancellationToken).ConfigureAwait(false);
        await realtimeNotifier.BroadcastLeaderboardUpdatedAsync(leaderboard.Entries, cancellationToken).ConfigureAwait(false);
    }

    private LiveSessionSnapshot BuildSnapshot(WalkingSession session, IReadOnlyList<WalkingSessionPoint> points)
    {
        var lastPoint = points[^1];
        var tail = points
            .TakeLast(20)
            .Select(x => x.ToResponse())
            .ToList();

        return new LiveSessionSnapshot(
            session.Id,
            session.UserId,
            session.User.DisplayName,
            session.User.AccentColor,
            session.Name,
            session.Status.ToString().ToLowerInvariant(),
            lastPoint.Latitude,
            lastPoint.Longitude,
            session.TotalDistanceMeters,
            session.TotalSteps,
            session.CaloriesBurned,
            lastPoint.RecordedAt,
            tail);
    }

    private int CalculateDurationSeconds(WalkingSession session, DateTimeOffset currentAt)
    {
        var activeSeconds = (int)Math.Max(0, (currentAt - session.StartedAt).TotalSeconds);
        activeSeconds -= session.PausedDurationSeconds;

        if (session.Status == WalkingSessionStatus.Paused && session.PausedAt is not null)
        {
            activeSeconds -= (int)Math.Max(0, (currentAt - session.PausedAt.Value).TotalSeconds);
        }

        return Math.Max(0, activeSeconds);
    }

    private async Task<WalkingSession> LoadSessionWithPointsAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.WalkingSessions
            .Include(x => x.Points)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            throw new AppException(404, "Walking session not found.", "session_not_found");
        }

        return session;
    }

    private async Task<WalkingSession> LoadSessionWithPointsAndUserAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.WalkingSessions
            .Include(x => x.User)
            .Include(x => x.Points)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            throw new AppException(404, "Walking session not found.", "session_not_found");
        }

        return session;
    }
}
