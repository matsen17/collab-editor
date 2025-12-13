using System.Text.Json.Serialization;

namespace CollabEditor.Infrastructure.WebSockets.Models;

public abstract record ClientMessage : WebSocketMessage;

public sealed record JoinMessage : ClientMessage
{
    [JsonPropertyName("type")]
    public override string Type => "join";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("participantId")]
    public Guid ParticipantId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed record OperationMessage : ClientMessage
{
    [JsonPropertyName("type")]
    public override string Type => "operation";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("operation")]
    public OperationDto Operation { get; init; } = null!;
}

public sealed record LeaveMessage : ClientMessage
{
    [JsonPropertyName("type")]
    public override string Type => "leave";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("participantId")]
    public Guid ParticipantId { get; init; }
}

public sealed record PingMessage : ClientMessage
{
    [JsonPropertyName("type")]
    public override string Type => "ping";
}

public record OperationDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    [JsonPropertyName("position")]
    public int Position { get; init; }
    
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    
    [JsonPropertyName("length")]
    public int? Length { get; init; }
    
    [JsonPropertyName("version")]
    public int Version { get; init; }
    
    [JsonPropertyName("authorId")]
    public Guid AuthorId { get; init; }
}