namespace StrideFlow.Application.Configuration;

public class ClientAppOptions
{
    public const string SectionName = "ClientApp";

    public string[] AllowedOrigins { get; init; } = ["http://localhost:5173"];
}
