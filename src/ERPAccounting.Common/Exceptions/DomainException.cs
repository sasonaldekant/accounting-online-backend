using System.Net;
using ERPAccounting.Common.Constants;

namespace ERPAccounting.Common.Exceptions;

public class DomainException : Exception
{
    public DomainException(
        HttpStatusCode statusCode,
        string title,
        string detail,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null,
        Exception? innerException = null)
        : base(detail, innerException)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        ErrorCode = errorCode ?? ErrorCodes.UnexpectedError;
        Errors = errors;
    }

    public HttpStatusCode StatusCode { get; }

    public string Title { get; }

    public string Detail { get; }

    public string ErrorCode { get; }

    public IDictionary<string, string[]>? Errors { get; }
}
