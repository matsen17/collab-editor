namespace CollabEditor.Application.Interfaces;

public interface IMessageBus
{
    /// <summary>
    /// Publish a message to a routing key.
    /// All subscribers with matching routing patterns will receive the message.
    /// </summary>
    Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default) 
        where T : IMessage;

    /// <summary>
    /// Subscribe to messages matching a routing pattern.
    /// Handler will be called for each received message.
    /// </summary>
    Task SubscribeAsync<T>(string routingPattern, Func<T, Task> handler, CancellationToken cancellationToken = default) 
        where T : IMessage;
}