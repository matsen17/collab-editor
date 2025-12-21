using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.Messages;
using CollabEditor.Infrastructure.WebSockets;

namespace CollabEditor.Infrastructure.Managers;

public sealed class SessionFlowManager(IWebSocketConnectionManager connectionManager) : 
    IHandle<ParticipantJoinedMessage>, 
    IHandle<ParticipantLeftMessage>
        
{
    public async Task HandleAsync(ParticipantJoinedMessage message, CancellationToken cancellationToken = default)
    {
        var sessionId = SessionId.From(message.Session.Id);
        var participantId = ParticipantId.From(message.ParticipantId);
        
        var joinedMessage = new WebSockets.Models.JoinedMessage
        {
            SessionId = message.Session.Id,
            ParticipantId = message.ParticipantId,
            Content = message.Session.Content,
            Version = message.Session.Version,
            Participants = message.Session.Participants
                .Select(p => new WebSockets.Models.ParticipantDto { Id = p.Id, Name = p.Name })
                .ToList()
        };

        await connectionManager.SendToParticipantAsync(participantId, joinedMessage);
        
        var broadcastMessage = new WebSockets.Models.ParticipantJoinedMessage
        {
            SessionId = message.Session.Id,
            ParticipantId = message.ParticipantId,
            Name = message.Name
        };

        await connectionManager.BroadcastToSessionAsync(sessionId, broadcastMessage, participantId);
    }

    public async Task HandleAsync(ParticipantLeftMessage message, CancellationToken cancellationToken = default)
    {
        var participantLeftMessage = new WebSockets.Models.ParticipantLeftMessage
        {
            SessionId = message.SessionId,
            ParticipantId = message.ParticipantId
        };
        
        await connectionManager.RemoveConnectionAsync(ParticipantId.From(message.ParticipantId));
        await connectionManager.BroadcastToSessionAsync(SessionId.From(message.SessionId), participantLeftMessage);
    }
}