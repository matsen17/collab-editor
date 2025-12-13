namespace CollabEditor.Domain.ValueObjects;

/// <summary>
/// Strong-typed identifier for edit sessions.
/// Prevents accidentally passing wrong ID types.
/// </summary>
public sealed record SessionId
{
    public Guid Value { get; }
    
    private SessionId(Guid value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Creates a new unique session ID.
    /// </summary>
    public static SessionId Create() => new SessionId(Guid.NewGuid());
    
    /// <summary>
    /// Reconstitutes a session ID from an existing Guid.
    /// Used when loading from storage.
    /// </summary>
    public static SessionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty", nameof(value));
        }
        
        return new SessionId(value);
    }
    
    /// <summary>
    /// Allows implicit conversion to Guid for convenience.
    /// </summary>
    public static implicit operator Guid(SessionId sessionId) => sessionId.Value;
    
    public override string ToString() => Value.ToString();
}