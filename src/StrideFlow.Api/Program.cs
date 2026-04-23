using Serilog;
using StrideFlow.Api.Definitions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddStrideFlowDefinitions();

    var app = builder.Build();
    await app.UseStrideFlowDefinitionsAsync().ConfigureAwait(false);

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception exception)
{
    Log.Fatal(exception, "StrideFlow API terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
