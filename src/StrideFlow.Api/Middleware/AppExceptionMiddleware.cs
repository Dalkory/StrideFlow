using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StrideFlow.Application.Common;

namespace StrideFlow.Api.Middleware;

public class AppExceptionMiddleware(RequestDelegate next, ILogger<AppExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            await WriteProblemDetailsAsync(context, exception.StatusCode, exception.Message, exception.ErrorCode, exception.Errors);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(item => item.ErrorMessage).Distinct().ToArray());

            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Validation failed.", "validation_failed", errors);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "An unexpected server error occurred.", "internal_error", null);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string detail,
        string? errorCode,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode >= 500 ? "Server error" : "Request failed",
            Detail = detail,
            Instance = context.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problem.Extensions["error_code"] = errorCode;
        }

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }));
    }
}
