using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Engagement;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/insights")]
public sealed class InsightsController(
    IInsightsService insightsService,
    ICurrentUserService currentUserService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await insightsService.GetAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }
}
