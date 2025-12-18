using CollabEditor.Domain.Common;
using CollabEditor.Domain.Events;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.Services;
using CollabEditor.Domain.ValueObjects;
using InvalidOperationException = CollabEditor.Domain.Exceptions.InvalidOperationException;

namespace CollabEditor.Domain.Aggregates.EditSessionAggregate;

/// <summary>
/// Aggregate root for collaborative editing sessions.
/// Enforces consistency rules around participants, content, and operations.
/// </summary>
public sealed class EditSession : AggregateRoot<SessionId>
{
    private readonly List<Participant> _participants = [];
    private readonly List<TextOperation> _operationHistory = [];
    
    public DocumentContent CurrentContent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastModifiedAt { get; private set; }
    public bool IsClosed { get; private set; }
    public int CurrentVersion { get; private set; }
    
    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<TextOperation> OperationHistory => _operationHistory.AsReadOnly();
    
    private EditSession(SessionId id) : base(id)
    {
        CurrentContent = DocumentContent.Empty();
    }
    
    private EditSession(SessionId id, DocumentContent initialContent, DateTime createdAt) : base(id)
    {
        CurrentContent = initialContent;
        CreatedAt = createdAt;
        LastModifiedAt = createdAt;
        IsClosed = false;
        CurrentVersion = 0;
    }
    
    public static EditSession Create(SessionId id, DocumentContent? initialContent = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        
        var content = initialContent ?? DocumentContent.Empty();
        var session = new EditSession(id, content, DateTime.UtcNow);
        
        // Note: We don't raise a SessionCreatedEvent here as creation happens in application layer
        // The application layer will handle persistence and event publishing
        
        return session;
    }
    
    public void AddParticipant(ParticipantId participantId, string name)
    {
        ArgumentNullException.ThrowIfNull(participantId);

        if (IsClosed)
        {
            throw new SessionClosedException(Id);
        }

        if (_participants.Any(p => p.Id == participantId))
        {
            throw new ParticipantAlreadyJoinedException(Id, participantId);
        }
        
        var participant = Participant.Create(participantId, name);
        _participants.Add(participant);
        
        RaiseDomainEvent(new ParticipantJoinedEvent
        {
            SessionId = Id,
            ParticipantId = participantId,
            ParticipantName = name,
            CurrentContent = CurrentContent,
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow
        });
    }
    
    public void RemoveParticipant(ParticipantId participantId)
    {
        ArgumentNullException.ThrowIfNull(participantId);
        
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        
        if (participant is null)
        {
            return;
        }
        
        _participants.Remove(participant);
        
        RaiseDomainEvent(new ParticipantLeftEvent
        {
            SessionId = Id,
            ParticipantId = participantId,
            RemainingParticipantCount = _participants.Count,
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow
        });
        
        if (_participants.Count == 0)
        {
            Close();
        }
    }
    
    public void ApplyOperation(TextOperation operation, IOperationalTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(transformer);

        if (IsClosed)
        {
            throw new SessionClosedException(Id);
        }
        
        if (_participants.All(p => p.Id != operation.AuthorId))
        {
            throw new InvalidOperationException(
                $"Participant {operation.AuthorId} is not in session",
                "PARTICIPANT_NOT_IN_SESSION");
        }
        
        // Transform operation against concurrent operations
        var transformedOp = TransformOperation(operation, transformer);
        
        // Apply the transformed operation to content
        CurrentContent = ApplyOperationToContent(transformedOp);
        
        // Update version and history
        CurrentVersion++;
        _operationHistory.Add(transformedOp);
        LastModifiedAt = DateTime.UtcNow;
        
        // Update participant activity
        var participant = _participants.First(p => p.Id == operation.AuthorId);
        participant.UpdateActivity();
        
        RaiseDomainEvent(new OperationAppliedEvent
        {
            SessionId = Id,
            Operation = transformedOp,
            ResultingContent = CurrentContent,
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow
        });
    }
    
    public void Close()
    {
        if (IsClosed)
        {
            return;
        }
        
        IsClosed = true;
        LastModifiedAt = DateTime.UtcNow;
        
        // Mark all participants as inactive
        foreach (var participant in _participants)
        {
            participant.MarkAsInactive();
        }
    }
    
    public void Reopen()
    {
        IsClosed = false;
        LastModifiedAt = DateTime.UtcNow;
    }
    
    public void UpdateParticipantActivity(ParticipantId participantId)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        
        if (participant is null)
        {
            throw new InvalidOperationException(
                $"Participant {participantId} not found in session",
                "PARTICIPANT_NOT_FOUND");
        }
        
        participant.UpdateActivity();
    }
    
    private TextOperation TransformOperation(
        TextOperation operation,
        IOperationalTransformer transformer)
    {
        // Get all operations that happened after the operation's version
        var concurrentOps = _operationHistory
            .Where(op => op.Version >= operation.Version)
            .ToList();
        
        if (!concurrentOps.Any())
        {
            return operation; // No concurrent operations, use as-is
        }
        
        return transformer.TransformAgainstMultiple(operation, concurrentOps);
    }
    
    private DocumentContent ApplyOperationToContent(TextOperation operation)
    {
        return operation.Type switch
        {
            OperationType.Insert => CurrentContent.InsertAt(
                operation.Position, 
                operation.Text ?? string.Empty),
            
            OperationType.Delete => CurrentContent.DeleteAt(
                operation.Position, 
                operation.Length ?? 0),
            
            _ => throw new InvalidOperationException(
                $"Unknown operation type: {operation.Type}",
                "INVALID_OPERATION_TYPE")
        };
    }
}