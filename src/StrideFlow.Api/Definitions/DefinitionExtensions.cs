using System.Reflection;
using Microsoft.Extensions.Logging;

namespace StrideFlow.Api.Definitions;

public static class DefinitionExtensions
{
    public static WebApplicationBuilder AddStrideFlowDefinitions(this WebApplicationBuilder builder, params Assembly[] assemblies)
    {
        var definitions = DiscoverDefinitions(assemblies.Length > 0 ? assemblies : [typeof(DefinitionExtensions).Assembly]);

        foreach (var definition in definitions)
        {
            definition.ConfigureServices(builder);
        }

        builder.Services.AddSingleton<IReadOnlyList<AppDefinition>>(definitions);
        return builder;
    }

    public static async Task UseStrideFlowDefinitionsAsync(this WebApplication app)
    {
        var definitions = app.Services.GetRequiredService<IReadOnlyList<AppDefinition>>();
        var logger = app.Services.GetRequiredService<ILogger<AppDefinition>>();

        foreach (var definition in definitions)
        {
            await definition.ConfigureApplicationAsync(app).ConfigureAwait(false);
        }

        logger.LogInformation("StrideFlow definitions applied: {Count}", definitions.Count);
    }

    private static IReadOnlyList<AppDefinition> DiscoverDefinitions(IReadOnlyCollection<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.ExportedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(AppDefinition).IsAssignableFrom(type))
            .Select(type => (AppDefinition)Activator.CreateInstance(type)!)
            .Where(definition => definition.Enabled)
            .OrderBy(definition => definition.OrderIndex)
            .ThenBy(definition => definition.GetType().FullName)
            .ToArray();
    }
}
