using ERPAccounting.Common.Exceptions;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.API.Filters;

/// <summary>
/// Globalni exception filter koji hvata concurrency exception-e i vraća standardizovani 409 Conflict response.
/// </summary>
public class ConcurrencyExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ConcurrencyExceptionFilter> _logger;

    public ConcurrencyExceptionFilter(ILogger<ConcurrencyExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            // Domain-level concurrency exception
            case ConflictException conflictEx:
                HandleConflictException(context, conflictEx);
                break;

            // EF Core concurrency exception
            case DbUpdateConcurrencyException dbConcurrencyEx:
                HandleDbConcurrencyException(context, dbConcurrencyEx);
                break;
        }
    }

    private void HandleConflictException(ExceptionContext context, ConflictException exception)
    {
        _logger.LogWarning(
            exception,
            "Concurrency conflict detected: ResourceType={ResourceType}, ResourceId={ResourceId}, ExpectedETag={ExpectedETag}, CurrentETag={CurrentETag}",
            exception.ResourceType,
            exception.ResourceId,
            exception.ExpectedETag,
            exception.CurrentETag);

        var concurrencyErrors = new List<string>
        {
            $"Resource '{exception.ResourceType ?? "Resource"}' has been modified by another user.",
            $"Expected ETag: {exception.ExpectedETag ?? "N/A"}",
            $"Current ETag: {exception.CurrentETag ?? "N/A"}",
            "Please refresh the entity and try again."
        };

        if (!string.IsNullOrWhiteSpace(exception.ResourceId))
        {
            concurrencyErrors.Insert(1, $"Resource ID: {exception.ResourceId}");
        }

        var problemDetails = ProblemDetailsDto.FromException(
            exception,
            context.HttpContext.Request.Path)
            with
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["concurrency"] = concurrencyErrors.ToArray()
                }
            };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status409Conflict
        };

        // Dodaj Current ETag u header kako bi klijent mogao odmah refresh-ovati
        if (!string.IsNullOrWhiteSpace(exception.CurrentETag))
        {
            context.HttpContext.Response.Headers["ETag"] = $"\"{exception.CurrentETag}\"";
        }

        context.ExceptionHandled = true;
    }

    private void HandleDbConcurrencyException(ExceptionContext context, DbUpdateConcurrencyException exception)
    {
        _logger.LogWarning(
            exception,
            "Database concurrency exception detected");

        var problemDetails = ProblemDetailsDto.Create(
            StatusCodes.Status409Conflict,
            "Concurrency Conflict",
            "The record has been modified by another user since it was retrieved.",
            context.HttpContext.Request.Path,
            "DATABASE_CONCURRENCY_CONFLICT",
            new Dictionary<string, string[]>
            {
                ["concurrency"] = new[]
                {
                    "A concurrency conflict occurred while saving changes.",
                    "The record may have been modified or deleted by another user.",
                    "Please refresh the entity and try again."
                }
            });

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status409Conflict
        };

        context.ExceptionHandled = true;
    }
}
