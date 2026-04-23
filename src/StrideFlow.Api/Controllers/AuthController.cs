using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StrideFlow.Api.Definitions;
using StrideFlow.Api.Extensions;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Models.Auth;

namespace StrideFlow.Api.Controllers;

[Route("api/auth")]
[EnableRateLimiting(RateLimiterDefinition.AuthPolicy)]
public class AuthController(IAuthService authService, ICurrentUserService currentUserService) : ApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAuthAsync(authService.RegisterAsync(request, HttpContext.ToClientContext(), cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAuthAsync(authService.LoginAsync(request, HttpContext.ToClientContext(request.DeviceName), cancellationToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAuthAsync(authService.RefreshAsync(request, HttpContext.ToClientContext(), cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(
            currentUserService.GetRequiredUserId(),
            currentUserService.GetCurrentJwtId(),
            request,
            HttpContext.ToClientContext(),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var response = await authService.GetCurrentUserAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    private async Task<IActionResult> ExecuteAuthAsync(Task<StrideFlow.Application.Models.Auth.AuthResponse> task)
    {
        var response = await task;
        return Ok(response);
    }
}
