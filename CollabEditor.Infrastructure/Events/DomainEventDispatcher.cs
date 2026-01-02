using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Mappers;
using CollabEditor.Domain.Common;
using CollabEditor.Domain.Events;
using CollabEditor.Infrastructure.Messages;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEditSessionRepository _repository;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IEditSessionRepository repository, 
        IMessageBus messageBus, 
        ILogger<DomainEventDispatcher> logger)
    {
        _repository = repository;
        _messageBus = messageBus;
        _logger = logger;
    }
    
    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await (domainEvent switch
            {
                ParticipantJoinedEvent e => HandleParticipantJoinedAsync(e, cancellationToken),
                ParticipantLeftEvent e => HandleParticipantLeftAsync(e, cancellationToken),
                OperationAppliedEvent e => HandleOperationAppliedAsync(e, cancellationToken),
            });

            _logger.LogDebug(
                "Successfully dispatched domain event: {EventType}",
                domainEvent.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error dispatching domain event {EventType}",
                domainEvent.GetType().Name);
            throw;
        }
    }
    
    private async Task HandleParticipantJoinedAsync(
        ParticipantJoinedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(domainEvent.SessionId, cancellationToken);

        if (session is null)
        {
            _logger.LogWarning(
                "Session {SessionId} not found when dispatching ParticipantJoinedEvent",
                domainEvent.SessionId);
            
            return;
        }
        
        var message = new ParticipantJoinedMessage
        {
            ParticipantId = domainEvent.ParticipantId.Value,
            Session = session.FromDomain(),
            Name = domainEvent.ParticipantName
        };

        await _messageBus.PublishAsync(
            ParticipantJoinedMessage.RoutingKey,
            message,
            cancellationToken);

        _logger.LogInformation(
            "Published ParticipantJoinedMessage for {ParticipantId} in session {SessionId}",
            domainEvent.ParticipantId,
            domainEvent.SessionId);
    }

    private async Task HandleParticipantLeftAsync(
        ParticipantLeftEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var message = new ParticipantLeftMessage
        {
            ParticipantId = domainEvent.ParticipantId.Value,
            SessionId = domainEvent.SessionId.Value
        };

        await _messageBus.PublishAsync(
            ParticipantLeftMessage.RoutingKey,
            message,
            cancellationToken);

        _logger.LogInformation(
            "Published ParticipantLeftMessage for {ParticipantId} in session {SessionId}",
            domainEvent.ParticipantId,
            domainEvent.SessionId);
    }

    private async Task HandleOperationAppliedAsync(
        OperationAppliedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var message = new OperationAppliedMessage
        {
            SessionId = domainEvent.SessionId.Value,
            Timestamp = domainEvent.OccurredAt,
            Type = domainEvent.Operation.Type.ToString().ToLowerInvariant(),
            Position = domainEvent.Operation.Position,
            Text = domainEvent.Operation.Text,
            Length = domainEvent.Operation.Length,
            Version = domainEvent.Operation.Version,
            AuthorId = domainEvent.Operation.AuthorId.Value
        };

        await _messageBus.PublishAsync(
            OperationAppliedMessage.RoutingKey,
            message,
            cancellationToken);

        _logger.LogInformation(
            "Published OperationAppliedMessage for session {SessionId}, version {Version}",
            domainEvent.SessionId,
            domainEvent.Operation.Version);
    }
}