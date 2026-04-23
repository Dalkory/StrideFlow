using StrideFlow.Application.Models.Engagement;

namespace StrideFlow.Application.Abstractions.Engagement;

public interface IRewardsService
{
    Task<RewardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);
}
