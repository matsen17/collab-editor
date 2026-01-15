using CollabEditor.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using ErrorContext = CollabEditor.API.ErrorHandling.Models.ErrorContext;

namespace CollabEditor.API.ErrorHandling;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        var context = ErrorContext.FromHttpContext(httpContext);
        
        logger.LogWarning(
            domainException,
            "Domain exception {ErrorCode} in {HttpMethod} {Path}: {Message}. CorrelationId: {CorrelationId}",
            domainException.ErrorCode,
            context.HttpMethod,
            context.RequestPath,
            domainException.Message,
            context.CorrelationId);

        var response = ErrorResponseFactory.CreateFromDomainException(domainException, context);
        var statusCode = ErrorResponseFactory.GetStatusCode(domainException.ErrorCode);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
