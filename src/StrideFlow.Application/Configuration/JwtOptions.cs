using System.ComponentModel.DataAnnotations;

namespace StrideFlow.Application.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "StrideFlow";

    [Required]
    public string Audience { get; init; } = "StrideFlow.Client";

    [Required]
    [MinLength(32)]
    public string Key { get; init; } = "please-change-this-development-key-1234567890";

    [Range(5, 240)]
    public int AccessTokenLifetimeMinutes { get; init; } = 20;

    [Range(1, 120)]
    public int RefreshTokenLifetimeDays { get; init; } = 30;
}
