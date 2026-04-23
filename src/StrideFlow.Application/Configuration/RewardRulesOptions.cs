using System.ComponentModel.DataAnnotations;

namespace StrideFlow.Application.Configuration;

public class RewardRulesOptions
{
    public const string SectionName = "RewardRules";

    public List<RewardTierOptions> Weekly { get; init; } = [];

    public List<RewardTierOptions> Monthly { get; init; } = [];
}

public class RewardTierOptions
{
    [Range(1, 100)]
    public int Rank { get; init; }

    [Range(1, 10000)]
    public int TelegramStars { get; init; }
}
