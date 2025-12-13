using System.Net.WebSockets;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets.Models;

namespace CollabEditor.Infrastructure.WebSockets;

public interface IWebSocketConnectionManager
{
    Task AddConnectionAsync(WebSocket webSocket, SessionId sessionId, ParticipantId participantId);
    Task RemoveConnectionAsync(ParticipantId participantId);
    Task BroadcastToSessionAsync(SessionId sessionId, ServerMessage message, ParticipantId? excludeParticipant = null);
    Task SendToParticipantAsync(ParticipantId participantId, ServerMessage message);
    int GetConnectionCount(SessionId sessionId);
}

public sealed class WebSocketConnectionManager : IWebSocketConnectionManager
{
    public Task AddConnectionAsync(WebSocket webSocket, SessionId sessionId, ParticipantId participantId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveConnectionAsync(ParticipantId participantId)
    {
        throw new NotImplementedException();
    }

    public Task BroadcastToSessionAsync(SessionId sessionId, ServerMessage message, ParticipantId? excludeParticipant = null)
    {
        throw new NotImplementedException();
    }

    public Task SendToParticipantAsync(ParticipantId participantId, ServerMessage message)
    {
        throw new NotImplementedException();
    }

    public int GetConnectionCount(SessionId sessionId)
    {
        throw new NotImplementedException();
    }
}