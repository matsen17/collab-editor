namespace CollabEditor.Domain.ValueObjects;

/// <summary>
/// Strong-typed identifier for participants in edit sessions.
/// </summary>
public sealed record ParticipantId
{
    public Guid Value { get; }
    
    private ParticipantId(Guid value)
    {
        Value = value;
    }
    
    public static ParticipantId Create() => new(Guid.NewGuid());
    
    public static ParticipantId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ParticipantId cannot be empty", nameof(value));
        }
        
        return new ParticipantId(value);
    }
    
    public static implicit operator Guid(ParticipantId participantId) => participantId.Value;
    
    public override string ToString() => Value.ToString();
}