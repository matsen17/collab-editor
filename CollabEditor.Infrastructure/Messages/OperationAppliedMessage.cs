using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Models;

namespace CollabEditor.Infrastructure.Messages;

public sealed record OperationAppliedMessage : IMessage
{
    public static string RoutingKey => "session.operations";
    
    public required Guid SessionId { get; init; }
    
    public required DateTime Timestamp { get; init; }

    public required string Type { get; init; }

    public required int Position { get; init; }
    
    public required string? Text { get; init; }
    
    public required int? Length { get; init; }
    
    public required int Version { get; init; }
    
    public required Guid AuthorId { get; init; }
}