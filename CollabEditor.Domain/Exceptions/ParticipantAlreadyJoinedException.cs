using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Exceptions;

/// <summary>
/// Thrown when a participant tries to join a session they're already in.
/// </summary>
public sealed class ParticipantAlreadyJoinedException(SessionId sessionId, ParticipantId participantId) : DomainException(
    $"Participant '{participantId}' has already joined session '{sessionId}'.",
    "PARTICIPANT_ALREADY_JOINED")
{
    public ParticipantId ParticipantId { get; } = participantId;
    
    public SessionId SessionId { get; } = sessionId;
}