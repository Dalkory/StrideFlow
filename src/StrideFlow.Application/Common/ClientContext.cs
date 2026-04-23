namespace StrideFlow.Application.Common;

public sealed record ClientContext(
    string? IpAddress,
    string? UserAgent,
    string? DeviceName);
