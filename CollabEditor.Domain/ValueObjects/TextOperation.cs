namespace CollabEditor.Domain.ValueObjects;

/// <summary>
/// Represents a single text editing operation (insert or delete).
/// Used for operational transformation.
/// </summary>
public sealed record TextOperation
{
    public required OperationType Type { get; init; }
    public required int Position { get; init; }
    public required string? Text { get; init; }   
    public required int? Length { get; init; }    
    public required int Version { get; init; }    
    public required ParticipantId AuthorId { get; init; }
    public required DateTime Timestamp { get; init; }
    
    private TextOperation() { }
    
    /// <summary>
    /// Creates an Insert operation.
    /// </summary>
    public static TextOperation Insert(
        int position, 
        string text, 
        int version,
        ParticipantId authorId)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(
                nameof(position), 
                "Position cannot be negative");
        
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException(
                "Insert text cannot be null or empty", 
                nameof(text));
        
        if (version < 0)
            throw new ArgumentOutOfRangeException(
                nameof(version), 
                "Version cannot be negative");
        
        ArgumentNullException.ThrowIfNull(authorId);
        
        return new TextOperation
        {
            Type = OperationType.Insert,
            Position = position,
            Text = text,
            Length = null,
            Version = version,
            AuthorId = authorId,
            Timestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Creates a Delete operation.
    /// </summary>
    public static TextOperation Delete(
        int position, 
        int length, 
        int version,
        ParticipantId authorId)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Position cannot be negative");
        }
        
        if (length <= 0)
        {
            throw new ArgumentException(
                "Delete length must be positive",
                nameof(length));
        }
        
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                "Version cannot be negative");
        }
        
        ArgumentNullException.ThrowIfNull(authorId);
        
        return new TextOperation
        {
            Type = OperationType.Delete,
            Position = position,
            Text = null,
            Length = length,
            Version = version,
            AuthorId = authorId,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public override string ToString()
    {
        return Type switch
        {
            OperationType.Insert => $"Insert '{Text}' at {Position} (v{Version})",
            OperationType.Delete => $"Delete {Length} chars at {Position} (v{Version})",
            _ => base.ToString()
        };
    }
}