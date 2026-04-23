using Microsoft.Extensions.Options;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Models.Ads;

namespace StrideFlow.Infrastructure.Services;

public class AdService(IOptions<AdSlotsOptions> adSlotsOptions) : IAdService
{
    public Task<IReadOnlyList<AdSlotResponse>> GetSlotsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AdSlotResponse> response = adSlotsOptions.Value.Slots
            .Select(x => new AdSlotResponse(
                x.Key,
                x.Placement,
                x.Title,
                x.Description,
                x.Enabled,
                x.IsPlaceholder))
            .ToList();

        return Task.FromResult(response);
    }
}
