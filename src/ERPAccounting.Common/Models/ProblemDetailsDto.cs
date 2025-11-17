using ERPAccounting.Common.Exceptions;

namespace ERPAccounting.Common.Models;

public record class ProblemDetailsDto
{
    public int Status { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public string? Instance { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ProblemDetailsDto Create(
        int status,
        string title,
        string detail,
        string? instance = null,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null)
        => new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = instance,
            ErrorCode = errorCode ?? status.ToString(),
            Errors = errors
        };

    public static ProblemDetailsDto FromException(DomainException exception, string? instance = null)
        => new()
        {
            Status = (int)exception.StatusCode,
            Title = exception.Title,
            Detail = exception.Detail,
            ErrorCode = exception.ErrorCode,
            Instance = instance,
            Errors = exception.Errors
        };
}
