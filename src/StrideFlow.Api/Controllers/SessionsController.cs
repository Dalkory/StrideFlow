using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StrideFlow.Api.Definitions;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Tracking;
using StrideFlow.Application.Models.Sessions;

namespace StrideFlow.Api.Controllers;

[Authorize]
[Route("api/sessions")]
public class SessionsController(ITrackingService trackingService, ICurrentUserService currentUserService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await trackingService.StartAsync(currentUserService.GetRequiredUserId(), request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var response = await trackingService.GetHistoryAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var response = await trackingService.GetCurrentAsync(currentUserService.GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetById(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await trackingService.GetByIdAsync(currentUserService.GetRequiredUserId(), sessionId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{sessionId:guid}/points")]
    [EnableRateLimiting(RateLimiterDefinition.TrackingPolicy)]
    public async Task<IActionResult> AddPoints(Guid sessionId, [FromBody] TrackSessionPointsRequest request, CancellationToken cancellationToken)
    {
        var response = await trackingService.AddPointsAsync(currentUserService.GetRequiredUserId(), sessionId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{sessionId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await trackingService.PauseAsync(currentUserService.GetRequiredUserId(), sessionId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{sessionId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await trackingService.ResumeAsync(currentUserService.GetRequiredUserId(), sessionId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{sessionId:guid}/stop")]
    public async Task<IActionResult> Stop(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await trackingService.StopAsync(currentUserService.GetRequiredUserId(), sessionId, cancellationToken);
        return Ok(response);
    }
}
