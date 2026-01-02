using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Models;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Infrastructure.Messages;

public sealed record ParticipantJoinedMessage : IMessage
{
    public static string RoutingKey =>  "session.participant.joined";
    
    public required Guid ParticipantId { get; init; }
    
    public required SessionDto Session { get; init; }
    
    public required string Name { get; init; }
}

public sealed record ParticipantLeftMessage : IMessage
{
    public static string RoutingKey =>  "session.participant.left";
    
    public required Guid ParticipantId { get; init; }
    
    public required Guid SessionId { get; init; }
}