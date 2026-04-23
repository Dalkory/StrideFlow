using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Users;
using StrideFlow.Application.Models.Users;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/profile")]
public class ProfileController(IUserService userService, ICurrentUserService currentUserService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await userService.GetProfileAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfileAsync(currentUserService.GetRequiredUserId(), request, cancellationToken);
        return Ok(response);
    }
}
