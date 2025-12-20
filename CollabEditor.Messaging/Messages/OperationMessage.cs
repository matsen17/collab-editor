namespace CollabEditor.Messaging.Messages;

public sealed class OperationMessage
{
    public Guid SessionId { get; set; }
    
    public OperationDto Operation { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
}

public sealed class OperationDto
{
    public string Type { get; set; } = string.Empty;
    
    public int Position { get; set; }
    
    public string? Text { get; set; }
    
    public int? Length { get; set; }
    
    public int Version { get; set; }
    
    public Guid AuthorId { get; set; }
}