using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Tracking;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/live")]
public class LiveController(ITrackingService trackingService) : ApiController
{
    [HttpGet("map")]
    public async Task<IActionResult> Map(CancellationToken cancellationToken)
    {
        var response = await trackingService.GetLiveMapAsync(cancellationToken);
        return Ok(response);
    }
}
