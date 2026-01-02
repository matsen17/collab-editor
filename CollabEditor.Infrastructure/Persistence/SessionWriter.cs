using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Infrastructure.Persistence;

public sealed class SessionWriter : ISessionWriter
{
    private readonly IEditSessionRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<SessionWriter> _logger;

    public SessionWriter(IEditSessionRepository repository, IDomainEventDispatcher eventDispatcher, ILogger<SessionWriter> logger)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task SaveAsync(EditSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        
        var domainEvents = session.DomainEvents.ToList();
        
        if (!domainEvents.Any())
        {
            _logger.LogDebug(
                "Saving session {SessionId} with no domain events",
                session.Id);
            
            await _repository.UpdateAsync(session, cancellationToken);
            return;
        }

        try
        {
            await _repository.UpdateAsync(session, cancellationToken);
            
            foreach (var domainEvent in domainEvents)
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
            
            session.ClearDomainEvents();

            _logger.LogInformation(
                "Saved session {SessionId} and published {EventCount} domain events",
                session.Id,
                domainEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error saving session {SessionId} or publishing events",
                session.Id);
            throw;
        }
    }
}