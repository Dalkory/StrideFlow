namespace StrideFlow.Application.Models.Dashboard;

public sealed record DailyTrendResponse(
    DateOnly Date,
    int Steps,
    double DistanceMeters,
    double CaloriesBurned);
