using Microsoft.Extensions.FileProviders;

namespace StrideFlow.Api.Definitions;

public sealed class SpaDefinition : AppDefinition
{
    public override int OrderIndex => 1000;

    public override Task ConfigureApplicationAsync(WebApplication app)
    {
        var clientDist = ResolveClientDistPath(app.Environment.ContentRootPath);
        if (!Directory.Exists(clientDist))
        {
            return Task.CompletedTask;
        }

        var fileProvider = new PhysicalFileProvider(clientDist);
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

        return Task.CompletedTask;
    }

    private static string ResolveClientDistPath(string contentRootPath)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(contentRootPath, "..", "StrideFlow.ClientApp", "dist")),
            Path.Combine(contentRootPath, "wwwroot", "spa")
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}
