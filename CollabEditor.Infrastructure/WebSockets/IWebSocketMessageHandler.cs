using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Infrastructure.WebSockets;

public interface IWebSocketMessageHandler
{
    Task HandleMessageAsync(string messageJson, ParticipantId participantId);
}