namespace StrideFlow.Application.Models.Ads;

public sealed record AdSlotResponse(
    string Key,
    string Placement,
    string Title,
    string Description,
    bool Enabled,
    bool IsPlaceholder);
