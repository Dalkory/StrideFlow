using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Users;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/ads")]
public class AdsController(IAdService adService) : ApiController
{
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots(CancellationToken cancellationToken)
    {
        var response = await adService.GetSlotsAsync(cancellationToken);
        return Ok(response);
    }
}
