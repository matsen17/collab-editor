using CollabEditor.Domain.Common;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Aggregates.EditSessionAggregate;

/// <summary>
/// Represents a participant in an edit session.
/// This is an entity within the EditSession aggregate.
/// </summary>
public sealed class Participant : Entity<ParticipantId>
{
    public string Name { get; private set; }
    
    public DateTime JoinedAt { get; private set; }
    
    public DateTime LastActiveAt { get; private set; }
    
    public bool IsActive { get; private set; }
    
    private Participant(ParticipantId id, string name, DateTime joinedAt) : base(id)
    {
        Name = name;
        JoinedAt = joinedAt;
        LastActiveAt = joinedAt;
        IsActive = true;
    }
    
    private Participant(
        ParticipantId id, 
        string name, 
        DateTime joinedAt, 
        DateTime lastActiveAt, 
        bool isActive) : base(id)
    {
        Id = id;
        Name = name;
        JoinedAt = joinedAt;
        LastActiveAt = lastActiveAt;
        IsActive = isActive;
    }
    
    public static Participant Create(ParticipantId id, string name)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Participant name cannot be empty", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Participant name cannot exceed 100 characters", nameof(name));
        }
        
        return new Participant(id, name, DateTime.UtcNow);
    }

    public static Participant FromPersistence(Guid id, string name, DateTime joinedAt, DateTime lastActiveAt,
        bool isActive) => new(ParticipantId.From(id),
        name,
        joinedAt,
        lastActiveAt,
        isActive);
    
    public void UpdateActivity()
    {
        LastActiveAt = DateTime.UtcNow;
        IsActive = true;
    }
    
    public void MarkAsInactive()
    {
        IsActive = false;
    }
    
    public void Reactivate()
    {
        IsActive = true;
        LastActiveAt = DateTime.UtcNow;
    }
}