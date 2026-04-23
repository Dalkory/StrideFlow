using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StrideFlow.Application.Abstractions.Auth;
using StrideFlow.Application.Abstractions.Realtime;
using StrideFlow.Application.Configuration;
using StrideFlow.Application.Validation.Auth;
using StrideFlow.Infrastructure;
using StrideFlow.Infrastructure.Database;
using StrideFlow.Api.Hubs;
using StrideFlow.Api.Infrastructure;
using StrideFlow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

builder.Services.AddStrideFlowInfrastructure(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers()
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

builder.Services.Configure<JsonOptions>(options => ConfigureJson(options.SerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfigureSwagger);

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection(ClientAppOptions.SectionName)
        .Get<ClientAppOptions>()?.AllowedOrigins ?? ["http://localhost:5173"];

    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", settings =>
    {
        settings.PermitLimit = 10;
        settings.Window = TimeSpan.FromMinutes(1);
        settings.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("tracking", settings =>
    {
        settings.PermitLimit = 40;
        settings.Window = TimeSpan.FromSeconds(10);
        settings.QueueLimit = 0;
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/activity"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
var signalR = builder.Services.AddSignalR().AddJsonProtocol(options => ConfigureJson(options.PayloadSerializerOptions));
if (!string.IsNullOrWhiteSpace(redisOptions?.ConnectionString))
{
    signalR.AddStackExchangeRedis(redisOptions.ConnectionString);
}

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app.Services);

app.UseSerilogRequestLogging();
app.UseMiddleware<AppExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ActivityHub>("/hubs/activity").RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
ConfigureSpa(app);

app.Run();

static void ConfigureJson(System.Text.Json.JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    options.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
}

static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StrideFlow API",
        Version = "v1",
        Description = "Monolithic pedometer platform with JWT auth, live route tracking, Redis-backed realtime updates and React frontend."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
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
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            []
        }
    });
}

static async Task ApplyDatabaseMigrationsAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<StrideFlowDbContext>();
    await dbContext.Database.MigrateAsync().ConfigureAwait(false);
}

static void ConfigureSpa(WebApplication app)
{
    var clientDist = ResolveClientDistPath(app.Environment.ContentRootPath);
    if (!Directory.Exists(clientDist))
    {
        return;
    }

    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientDist);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        RequestPath = string.Empty
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider
    });
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = fileProvider
    });
}

static string ResolveClientDistPath(string contentRootPath)
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "StrideFlow.ClientApp", "dist")),
        Path.Combine(contentRootPath, "wwwroot", "spa")
    };

    return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
}

public partial class Program;
