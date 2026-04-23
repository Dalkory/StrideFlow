using StrideFlow.Application.Models.Ads;

namespace StrideFlow.Application.Abstractions.Users;

public interface IAdService
{
    Task<IReadOnlyList<AdSlotResponse>> GetSlotsAsync(CancellationToken cancellationToken);
}
