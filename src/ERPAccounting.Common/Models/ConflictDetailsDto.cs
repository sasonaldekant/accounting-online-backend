using ERPAccounting.Common.Exceptions;

namespace ERPAccounting.Common.Models;

public sealed record class ConflictDetailsDto : ProblemDetailsDto
{
    public string? ResourceId { get; init; }
    public string? ResourceType { get; init; }
    public string? ExpectedETag { get; init; }
    public string? CurrentETag { get; init; }

    public static ConflictDetailsDto FromException(ConflictException exception, string? instance = null)
        => new()
        {
            Status = (int)exception.StatusCode,
            Title = exception.Title,
            Detail = exception.Detail,
            ErrorCode = exception.ErrorCode,
            Instance = instance,
            Errors = exception.Errors,
            ResourceId = exception.ResourceId,
            ResourceType = exception.ResourceType,
            ExpectedETag = exception.ExpectedETag,
            CurrentETag = exception.CurrentETag
        };
}
