using System.Text.Json.Serialization;

namespace CollabEditor.Infrastructure.WebSockets.Models;

public abstract record ServerMessage : WebSocketMessage;

public sealed record JoinedMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "joined";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("participantId")]
    public Guid ParticipantId { get; init; }
    
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
    
    [JsonPropertyName("version")]
    public int Version { get; init; }
    
    [JsonPropertyName("participants")]
    public List<ParticipantDto> Participants { get; init; } = new();
}

public sealed record ParticipantJoinedMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "participant-joined";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("participantId")]
    public Guid ParticipantId { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed record OperationAppliedMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "operation-applied";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("operation")]
    public OperationDto Operation { get; init; } = null!;
}

public sealed record ParticipantLeftMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "participant-left";
    
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }
    
    [JsonPropertyName("participantId")]
    public Guid ParticipantId { get; init; }
}

public sealed record ErrorMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "error";
    
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
    
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }
}

public sealed record PongMessage : ServerMessage
{
    [JsonPropertyName("type")]
    public override string Type => "pong";
}

public record ParticipantDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}