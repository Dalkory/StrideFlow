using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Users;

namespace StrideFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leaderboard")]
public class LeaderboardController(IUserService userService, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string period = "day",
        [FromQuery] int limit = 10,
        [FromQuery] string? city = null,
        CancellationToken cancellationToken = default)
    {
        var response = await userService.GetLeaderboardAsync(currentUserService.GetRequiredUserId(), period, limit, city, cancellationToken);
        return Ok(response);
    }
}
