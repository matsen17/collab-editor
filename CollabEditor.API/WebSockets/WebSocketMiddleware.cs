using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets;

namespace CollabEditor.API.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        IWebSocketConnectionManager connectionManager,
        IWebSocketMessageHandler messageHandler)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        // Extract participantId from query string
        if (!context.Request.Query.TryGetValue("participantId", out var participantIdStr) ||
            !Guid.TryParse(participantIdStr, out var participantIdGuid))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("participantId query parameter is required");
            return;
        }

        var participantId = ParticipantId.From(participantIdGuid);
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        _logger.LogInformation("WebSocket connection established for participant {ParticipantId}", participantId);

        try
        {
            await HandleWebSocketAsync(webSocket, participantId, connectionManager, messageHandler);
        }
        catch (DomainException domainEx)
        {
            _logger.LogWarning(
                domainEx,
                "Domain error in WebSocket for participant {ParticipantId}: {ErrorCode}",
                participantId,
                domainEx.ErrorCode);

            await SendErrorAndCloseAsync(webSocket, domainEx.Message, domainEx.ErrorCode, participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in WebSocket for participant {ParticipantId}", participantId);
            await SendErrorAndCloseAsync(webSocket, "Internal server error", "WEBSOCKET_ERROR", participantId);
        }
        finally
        {
            await connectionManager.RemoveConnectionAsync(participantId);
            
            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }
            
            _logger.LogInformation("WebSocket connection closed for participant {ParticipantId}", participantId);
        }
    }
    
    private async Task HandleWebSocketAsync(
        WebSocket webSocket,
        ParticipantId participantId,
        IWebSocketConnectionManager connectionManager,
        IWebSocketMessageHandler messageHandler)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State is WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            if (result.MessageType is WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client requested close",
                    CancellationToken.None);
                break;
            }

            if (result.MessageType is WebSocketMessageType.Text)
            {
                var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // Parse to get sessionId for connection tracking
                // This is a simplified approach - in production you'd want more robust parsing
                if (messageJson.Contains("\"type\":\"join\""))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(messageJson);
                    if (doc.RootElement.TryGetProperty("sessionId", out var sessionIdElement))
                    {
                        var sessionId = SessionId.From(sessionIdElement.GetGuid());
                        await connectionManager.AddConnectionAsync(webSocket, sessionId, participantId);
                    }
                }

                await messageHandler.HandleMessageAsync(messageJson, participantId);
            }
        }
    }

    private static async Task SendErrorAndCloseAsync(
        WebSocket webSocket,
        string message,
        string errorCode,
        ParticipantId participantId)
    {
        var errorResponse = new
        {
            type = "error",
            error = message,
            errorCode,
            participantId = participantId.Value
        };

        var errorJson = JsonSerializer.Serialize(errorResponse);

        // Only send message if WebSocket is still open
        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                await webSocket.SendAsync(
                    Encoding.UTF8.GetBytes(errorJson),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);
            }
            catch
            {
                // Ignore errors when sending error message
            }
        }

        // Close the connection if not already closed
        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    errorCode,
                    CancellationToken.None);
            }
            catch
            {
                // Ignore errors when closing
            }
        }
    }
}