using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets;
using OperationAppliedMessage = CollabEditor.Infrastructure.Messages.OperationAppliedMessage;
using ServerModels = CollabEditor.Infrastructure.WebSockets.Models;

namespace CollabEditor.Infrastructure.Managers;

public sealed class OperationFlowManager(IWebSocketConnectionManager connectionManager) 
    : IHandle<OperationAppliedMessage>
{
    public async Task HandleAsync(OperationAppliedMessage message, CancellationToken cancellationToken = default)
    {
        var sessionId = SessionId.From(message.SessionId);
        var connectionCount = connectionManager.GetConnectionCount(sessionId);
            
        if (connectionCount == 0)
        {
            return;
        }
        
        var broadcastMessage = new ServerModels.OperationAppliedMessage
        {
            SessionId = message.SessionId,
            Operation = new ServerModels.OperationDto
            {
                Type = message.Type,
                Position = message.Position,
                Text = message.Text,
                Length = message.Length,
                Version = message.Version,
                AuthorId = message.AuthorId
            }
        };

        await connectionManager.BroadcastToSessionAsync(sessionId, broadcastMessage);
    }
}