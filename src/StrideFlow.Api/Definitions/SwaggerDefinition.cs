using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace StrideFlow.Api.Definitions;

public sealed class SwaggerDefinition : AppDefinition
{
    public override int OrderIndex => -600;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(ConfigureSwagger);
    }

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((swagger, _) => swagger.Servers = []);
        });

        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "StrideFlow API";
            options.DefaultModelRendering(ModelRendering.Model);
            options.DefaultModelsExpandDepth(0);
            options.DocExpansion(DocExpansion.None);
            options.DisplayRequestDuration();
            options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
        });

        return Task.CompletedTask;
    }

    private static void ConfigureSwagger(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "StrideFlow API",
            Version = "v1",
            Description = "Monolithic pedometer platform with JWT auth, live route tracking, Redis-backed realtime updates and React frontend."
        });

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT access token."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                },
                []
            }
        });

        options.ResolveConflictingActions(actions => actions.First());
        options.CustomSchemaIds(type => type.FullName);
    }
}
