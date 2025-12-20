namespace CollabEditor.Application.Interfaces;

public interface IMessageBusFactory
{
    Task<IMessageBus> CreateAsync(CancellationToken cancellationToken = default);
}