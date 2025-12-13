namespace CollabEditor.Domain.Common;

/// <summary>
/// Base class for aggregate roots.
/// Aggregates are consistency boundaries - all changes go through the root.
/// They collect domain events that occurred during operations.
/// </summary>
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id)
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// Read-only collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Derived classes call this to record that something happened.
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    
    /// <summary>
    /// Infrastructure calls this after publishing events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}