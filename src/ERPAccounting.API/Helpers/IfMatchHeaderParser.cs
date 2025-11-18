using System;
using System.Collections.Generic;
using System.Linq;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.API.Helpers;

internal static class IfMatchHeaderParser
{
    public static bool TryExtractRowVersion(
        HttpContext httpContext,
        ILogger logger,
        string logContext,
        out byte[]? rowVersion,
        out ProblemDetailsDto? problemDetails)
    {
        rowVersion = null;
        problemDetails = null;

        var ifMatch = httpContext.Request.Headers["If-Match"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            logger.LogWarning("Missing If-Match header for {Context}", logContext);
            problemDetails = CreateProblem(httpContext, ErrorMessages.MissingIfMatchHeader);
            return false;
        }

        try
        {
            var etagValue = ifMatch.Trim('\"');
            rowVersion = Convert.FromBase64String(etagValue);
            return true;
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid ETag format for {Context}: {ETag}", logContext, ifMatch);
            problemDetails = CreateProblem(httpContext, ErrorMessages.InvalidIfMatchHeader);
            return false;
        }
    }

    private static ProblemDetailsDto CreateProblem(HttpContext httpContext, string detail)
    {
        return ProblemDetailsDto.Create(
            StatusCodes.Status400BadRequest,
            ErrorMessages.BadRequestTitle,
            detail,
            httpContext.TraceIdentifier,
            ErrorCodes.MissingIfMatchHeader,
            new Dictionary<string, string[]>
            {
                ["If-Match"] = new[] { detail }
            });
    }
}
