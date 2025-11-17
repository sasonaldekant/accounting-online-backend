using System.Net;
using ERPAccounting.Common.Constants;

namespace ERPAccounting.Common.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string detail, string? resourceId = null, string? resourceType = null)
        : base(HttpStatusCode.NotFound, ErrorMessages.NotFoundTitle, detail, ErrorCodes.ResourceNotFound)
    {
        ResourceId = resourceId;
        ResourceType = resourceType;
    }

    public string? ResourceId { get; }

    public string? ResourceType { get; }
}
