using StrideFlow.Application.Models.Engagement;

namespace StrideFlow.Application.Abstractions.Engagement;

public interface IInsightsService
{
    Task<ActivityInsightsResponse> GetAsync(Guid userId, CancellationToken cancellationToken);
}
