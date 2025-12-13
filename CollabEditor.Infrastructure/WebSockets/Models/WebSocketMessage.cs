using System.Text.Json.Serialization;

namespace CollabEditor.Infrastructure.WebSockets.Models;

public abstract record WebSocketMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}