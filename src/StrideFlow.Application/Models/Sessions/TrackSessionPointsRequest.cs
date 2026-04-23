namespace StrideFlow.Application.Models.Sessions;

public sealed record TrackSessionPointsRequest(IReadOnlyList<TrackPointRequest> Points);
