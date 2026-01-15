using CollabEditor.API.ErrorHandling;
using CollabEditor.API.Middleware;

namespace CollabEditor.API.Extensions;

/// <summary>
/// Extension methods for registering global error handling services.
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Registers global error handling middleware and services.
    /// </summary>
    public static IServiceCollection AddGlobalErrorHandling(this IServiceCollection services)
    {
        // Register the error response factory as a singleton (stateless)
        services.AddSingleton<ErrorResponseFactory>();

        // Register exception handlers in priority order
        // DomainExceptionHandler runs first, GlobalExceptionHandler is the fallback
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Enable ProblemDetails support for enhanced error responses
        services.AddProblemDetails();

        return services;
    }
}
