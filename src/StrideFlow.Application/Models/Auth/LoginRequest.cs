namespace StrideFlow.Application.Models.Auth;

public sealed record LoginRequest(
    string Email,
    string Password,
    string? DeviceName);
