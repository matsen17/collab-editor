using CollabEditor.Domain.Common;

namespace CollabEditor.Infrastructure.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}