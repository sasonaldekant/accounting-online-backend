using System.Net;
using ERPAccounting.Common.Constants;

namespace ERPAccounting.Common.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string detail, IDictionary<string, string[]> errors)
        : base(HttpStatusCode.BadRequest, ErrorMessages.ValidationFailedTitle, detail, ErrorCodes.ValidationFailed, errors)
    {
    }
}
