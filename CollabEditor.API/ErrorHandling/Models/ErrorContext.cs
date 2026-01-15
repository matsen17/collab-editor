namespace CollabEditor.API.ErrorHandling.Models;

public sealed class ErrorContext
{
    public required string CorrelationId { get; init; }
    
    public required string RequestPath { get; init; }
    
    public required string HttpMethod { get; init; }
    
    public Guid? SessionId { get; init; }
    
    public Guid? ParticipantId { get; init; }
    
    public required DateTimeOffset Timestamp { get; init; }
    
    public static ErrorContext FromHttpContext(HttpContext context)
    {
        var correlationId = CreateCorrelationId(context);
        var sessionId = TryExtractSessionId(context);
        var participantId = TryExtractParticipantId(context);

        return new ErrorContext
        {
            CorrelationId = correlationId,
            RequestPath = context.Request.Path,
            HttpMethod = context.Request.Method,
            SessionId = sessionId,
            ParticipantId = participantId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static Guid? TryExtractParticipantId(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("participantId", out var queryParticipantId) &&
            Guid.TryParse(queryParticipantId, out var parsedParticipantId))
        {
            return parsedParticipantId;
        }

        if (context.Request.RouteValues.TryGetValue("participantId", out var routeParticipantId) &&
            Guid.TryParse(routeParticipantId?.ToString(), out var parsedRouteParticipantId))
        {
            return parsedRouteParticipantId;
        }

        return null;
    }

    private static Guid? TryExtractSessionId(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("id", out var routeId) &&
            Guid.TryParse(routeId?.ToString(), out var parsedSessionId))
        {
            return parsedSessionId;
        }

        return null;
    }
    
    private static string CreateCorrelationId(HttpContext context) 
        => context.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
}
