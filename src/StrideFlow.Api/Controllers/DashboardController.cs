using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Users;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/dashboard")]
public class DashboardController(IUserService userService, ICurrentUserService currentUserService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await userService.GetDashboardAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }
}
