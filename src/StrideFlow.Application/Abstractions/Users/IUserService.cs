using StrideFlow.Application.Models.Dashboard;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.Application.Abstractions.Users;

public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken);

    Task<LeaderboardResponse> GetLeaderboardAsync(Guid userId, string period, int limit, string? city, CancellationToken cancellationToken);
}
