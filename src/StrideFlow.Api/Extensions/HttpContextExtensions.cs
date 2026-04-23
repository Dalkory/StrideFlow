using StrideFlow.Application.Common;

namespace StrideFlow.Api.Extensions;

public static class HttpContextExtensions
{
    public static ClientContext ToClientContext(this HttpContext httpContext, string? deviceName = null)
    {
        return new ClientContext(
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            string.IsNullOrWhiteSpace(deviceName) ? httpContext.Request.Headers["X-Device-Name"].ToString() : deviceName);
    }
}
