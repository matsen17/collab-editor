using CollabEditor.Domain.Common;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Events;

public sealed record ParticipantLeftEvent : DomainEvent
{
    public required SessionId SessionId { get; init; }
    
    public required ParticipantId ParticipantId { get; init; }
    
    public required int RemainingParticipantCount { get; init; }
}