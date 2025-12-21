namespace CollabEditor.Application.Interfaces;

public interface IHandle<in TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}