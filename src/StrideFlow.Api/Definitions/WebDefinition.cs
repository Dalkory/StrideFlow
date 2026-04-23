using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Request validation failed",
                        Detail = "One or more validation errors occurred.",
                        Instance = context.HttpContext.Request.Path
                    };
                    problem.Extensions["error_code"] = "validation_failed";

                    var result = new ObjectResult(problem)
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                    result.ContentTypes.Add("application/problem+json");

                    return result;
                };
            })
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => ConfigureJson(options.SerializerOptions));
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
