using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets.Models;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Infrastructure.WebSockets;

public sealed class WebSocketConnectionManager : IWebSocketConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocketConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, ImmutableHashSet<Guid>> _sessionParticipants = new();
    
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    public Task AddConnectionAsync(WebSocket webSocket, SessionId sessionId, ParticipantId participantId)
    {
        var connection = new WebSocketConnection(webSocket, sessionId, participantId);
        
        if (!_connections.TryAdd(participantId.Value, connection))
        {
            _logger.LogWarning("Connection already exists for participant {ParticipantId}", participantId);
            return Task.CompletedTask;
        }

        _sessionParticipants.AddOrUpdate(
            sessionId.Value, _ => ImmutableHashSet.Create(participantId.Value),
            (_, currentSet) => currentSet.Add(participantId.Value));
        
        _logger.LogInformation(
            "Added WebSocket connection for participant {ParticipantId} in session {SessionId}",
            participantId,
            sessionId);
        
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(ParticipantId participantId)
    {
        if (!_connections.TryRemove(participantId.Value, out var connection))
        {
            return Task.CompletedTask;
        }
        
        _sessionParticipants.AddOrUpdate(
            connection.SessionId.Value, 
            _ => ImmutableHashSet<Guid>.Empty, // Shouldn't happen, but safe
            (_, currentSet) =>
            {
                var newSet = currentSet.Remove(participantId.Value);
                return newSet.IsEmpty 
                    ? ImmutableHashSet<Guid>.Empty 
                    : newSet;
            });
        
        if (_sessionParticipants.TryGetValue(connection.SessionId.Value, out var participants) 
            && participants.IsEmpty)
        {
            _sessionParticipants.TryRemove(connection.SessionId.Value, out _);
        }
        
        _logger.LogInformation(
            "Removed WebSocket connection for participant {ParticipantId}",
            participantId);

        return Task.CompletedTask;
    }

    public async Task BroadcastToSessionAsync(SessionId sessionId, ServerMessage message, ParticipantId? excludeParticipant = null)
    {
        if (!_sessionParticipants.TryGetValue(sessionId.Value, out var participantIds))
        {
            // No one in this session
            return;
        }
        
        var json = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        
        var tasks = participantIds
            .Where(id => excludeParticipant is null || id != excludeParticipant.Value)
            .Select(async participantId =>
            {
                if (_connections.TryGetValue(participantId, out var connection))
                {
                    await SendWebSocketMessage(connection, buffer, participantId);
                }
            });
        await Task.WhenAll(tasks);
    }

    public async Task SendToParticipantAsync(ParticipantId participantId, ServerMessage message)
    {
        if (!_connections.TryGetValue(participantId.Value, out var connection))
        {
            _logger.LogWarning("Connection not found for participant {ParticipantId}", participantId);
            return;
        }
        
        var json = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        await SendWebSocketMessage(connection, buffer, participantId.Value);
    }

    public int GetConnectionCount(SessionId sessionId)
    {
        return _sessionParticipants.TryGetValue(sessionId.Value, out var participants) 
            ? participants.Count
            : 0;
    }

    public async Task CloseConnectionAsync(ParticipantId participantId, string reason)
    {
        if (!_connections.TryGetValue(participantId.Value, out var connection))
        {
            return;
        }

        try
        {
            if (connection.WebSocket.State is WebSocketState.Open)
            {
                await connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing WebSocket for participant {ParticipantId}", participantId);
        }
        finally
        {
            await RemoveConnectionAsync(participantId);
        }
    }

    private async Task SendWebSocketMessage(WebSocketConnection connection, byte[] buffer, Guid participantId)
    {
        try
        {
            if (connection.WebSocket.State is WebSocketState.Open)
            {
                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error sending to {ParticipantId}", participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending to {ParticipantId}", participantId);
        }
    }
    
    private sealed record WebSocketConnection(
        WebSocket WebSocket,
        SessionId SessionId,
        ParticipantId ParticipantId);
}