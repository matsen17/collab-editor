using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Infrastructure.WebSockets;

public interface IWebSocketMessageHandler
{
    Task HandleMessageAsync(string messageJson, ParticipantId participantId);
}

public sealed class WebSocketMessageHandler : IWebSocketMessageHandler
{
    public Task HandleMessageAsync(string messageJson, ParticipantId participantId)
    {
        throw new NotImplementedException();
    }
}