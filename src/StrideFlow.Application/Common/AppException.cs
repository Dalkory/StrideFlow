namespace StrideFlow.Application.Common;

public sealed class AppException : Exception
{
    public AppException(int statusCode, string message, string? errorCode = null, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string? ErrorCode { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
