namespace StrideFlow.Application.Configuration;

public class AdSlotsOptions
{
    public const string SectionName = "AdSlots";

    public List<AdSlotOptions> Slots { get; init; } = [];
}

public class AdSlotOptions
{
    public string Key { get; init; } = string.Empty;

    public string Placement { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;

    public bool IsPlaceholder { get; init; } = true;
}
