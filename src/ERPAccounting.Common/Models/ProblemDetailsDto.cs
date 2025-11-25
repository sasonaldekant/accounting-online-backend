using ERPAccounting.Common.Exceptions;

namespace ERPAccounting.Common.Models;

public record class ProblemDetailsDto
{
    /// <summary>
    /// A URI reference that identifies the problem type (RFC 7807)
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Application-specific error code
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    public string? Instance { get; init; }

    /// <summary>
    /// Trace identifier for correlating logs
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Validation or other structured errors
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Additional properties for extending the problem details
    /// </summary>
    public IDictionary<string, object?>? Extensions { get; init; }

    public static ProblemDetailsDto Create(
        int status,
        string title,
        string detail,
        string? instance = null,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null,
        string? type = null,
        string? traceId = null,
        IDictionary<string, object?>? extensions = null)
        => new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = instance,
            ErrorCode = errorCode ?? status.ToString(),
            Errors = errors,
            Type = type,
            TraceId = traceId,
            Extensions = extensions
        };

    public static ProblemDetailsDto FromException(DomainException exception, string? instance = null, string? traceId = null)
        => new()
        {
            Status = (int)exception.StatusCode,
            Title = exception.Title,
            Detail = exception.Detail,
            ErrorCode = exception.ErrorCode,
            Instance = instance,
            Errors = exception.Errors,
            TraceId = traceId
        };
}
