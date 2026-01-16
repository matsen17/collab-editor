using Microsoft.AspNetCore.Diagnostics;
using ErrorContext = CollabEditor.API.ErrorHandling.Models.ErrorContext;

namespace CollabEditor.API.ErrorHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var context = ErrorContext.FromHttpContext(httpContext);
        
        logger.LogError(
            exception,
            "Unhandled exception {ExceptionType} in {HttpMethod} {Path}. CorrelationId: {CorrelationId}",
            exception.GetType().Name,
            context.HttpMethod,
            context.RequestPath,
            context.CorrelationId);

        // In Development, include exception details for debugging
        // In Production, hide details for security
        var includeDetails = environment.IsDevelopment();
        var response = ErrorResponseFactory.CreateFromException(exception, context, includeDetails);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        // Return true to indicate the exception has been handled
        return true;
    }
}
