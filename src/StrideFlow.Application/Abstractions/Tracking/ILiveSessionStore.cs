using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Application.Abstractions.Tracking;

public interface ILiveSessionStore
{
    Task UpsertAsync(LiveSessionSnapshot snapshot, CancellationToken cancellationToken);

    Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveSessionSnapshot>> GetAllAsync(CancellationToken cancellationToken);
}
