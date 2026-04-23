using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Application.Abstractions.Tracking;

public interface ITrackingService
{
    Task<WalkingSessionDetailResponse> StartAsync(Guid userId, StartSessionRequest request, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WalkingSessionResponse>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse> GetByIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse> AddPointsAsync(Guid userId, Guid sessionId, TrackSessionPointsRequest request, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse> PauseAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse> ResumeAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<WalkingSessionDetailResponse> StopAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveSessionResponse>> GetLiveMapAsync(CancellationToken cancellationToken);
}
