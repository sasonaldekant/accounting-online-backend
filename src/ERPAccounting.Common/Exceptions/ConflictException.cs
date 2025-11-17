using System.Net;
using ERPAccounting.Common.Constants;

namespace ERPAccounting.Common.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(
        string detail,
        string? resourceId = null,
        string? resourceType = null,
        string? expectedETag = null,
        string? currentETag = null)
        : base(HttpStatusCode.Conflict, ErrorMessages.ConflictTitle, detail, ErrorCodes.ConcurrencyConflict)
    {
        ResourceId = resourceId;
        ResourceType = resourceType;
        ExpectedETag = expectedETag;
        CurrentETag = currentETag;
    }

    public string? ResourceId { get; }

    public string? ResourceType { get; }

    public string? ExpectedETag { get; }

    public string? CurrentETag { get; }
}
