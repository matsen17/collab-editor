using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to perform operations on a closed session.
/// </summary>
public sealed class SessionClosedException(SessionId sessionId) : DomainException(
    $"Session '{sessionId}' is closed and cannot accept operations.",
    "SESSION_CLOSED")
{
    public SessionId SessionId { get; } = sessionId;
}