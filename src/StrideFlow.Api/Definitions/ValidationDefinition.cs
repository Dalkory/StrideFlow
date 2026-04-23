using FluentValidation;
using FluentValidation.AspNetCore;
using StrideFlow.Application.Validation.Auth;

namespace StrideFlow.Api.Definitions;

public sealed class ValidationDefinition : AppDefinition
{
    public override int OrderIndex => -800;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        builder.Services.AddFluentValidationAutoValidation();
    }
}
