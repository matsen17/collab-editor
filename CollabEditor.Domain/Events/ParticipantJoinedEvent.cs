using CollabEditor.Domain.Common;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Events;

public sealed record ParticipantJoinedEvent : DomainEvent
{
    public required SessionId SessionId { get; init; }
    
    public required ParticipantId ParticipantId { get; init; }
    
    public required string ParticipantName { get; init; }
    
    public required DocumentContent CurrentContent { get; init; }
}