using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.API.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseDomainExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;
                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DomainExceptionHandling");

                ProblemDetailsDto problem;

                if (exception is ConflictException conflictException)
                {
                    problem = ConflictDetailsDto.FromException(conflictException, context.TraceIdentifier);
                    context.Response.StatusCode = (int)conflictException.StatusCode;
                }
                else if (exception is DomainException domainException)
                {
                    problem = ProblemDetailsDto.FromException(domainException, context.TraceIdentifier);
                    context.Response.StatusCode = (int)domainException.StatusCode;
                }
                else
                {
                    logger.LogError(exception, "Unhandled exception occurred.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    problem = ProblemDetailsDto.Create(
                        StatusCodes.Status500InternalServerError,
                        ErrorMessages.UnhandledExceptionTitle,
                        ErrorMessages.UnhandledExceptionDetail,
                        context.TraceIdentifier,
                        ErrorCodes.UnexpectedError);
                }

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }
}
