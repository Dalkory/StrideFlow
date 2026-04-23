using System.ComponentModel.DataAnnotations;

namespace StrideFlow.Application.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; init; } = "localhost:6379";

    [Required]
    public string InstanceName { get; init; } = "strideflow";
}
