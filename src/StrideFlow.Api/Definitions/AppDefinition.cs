using Microsoft.AspNetCore.Builder;

namespace StrideFlow.Api.Definitions;

public abstract class AppDefinition
{
    public virtual int OrderIndex => 0;

    public virtual bool Enabled => true;

    public virtual void ConfigureServices(WebApplicationBuilder builder)
    {
    }

    public virtual Task ConfigureApplicationAsync(WebApplication app)
    {
        return Task.CompletedTask;
    }
}
