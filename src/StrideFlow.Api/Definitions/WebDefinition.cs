using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using StrideFlow.Api.Middleware;

namespace StrideFlow.Api.Definitions;

public sealed class WebDefinition : AppDefinition
{
    public override int OrderIndex => -700;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

        builder.Services.Configure<JsonOptions>(options => ConfigureJson(options.SerializerOptions));
    }

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.UseMiddleware<AppExceptionMiddleware>();
        app.UseHttpsRedirection();
        app.UseRouting();
        return Task.CompletedTask;
    }

    internal static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    }
}
