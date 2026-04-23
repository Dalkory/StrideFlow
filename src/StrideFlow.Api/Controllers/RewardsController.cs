using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Engagement;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/rewards")]
public sealed class RewardsController(
    IRewardsService rewardsService,
    ICurrentUserService currentUserService) : ApiController
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var response = await rewardsService.GetSummaryAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }
}
