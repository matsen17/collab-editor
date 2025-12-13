namespace CollabEditor.Domain.Common;

/// <summary>
/// Base record for all domain events.
/// Domain events represent something that happened in the domain.
/// They are immutable facts about the past.
/// </summary>
public abstract record DomainEvent
{
    public required Guid EventId { get; init; }
    
    public required DateTime OccurredAt { get; init; }
}