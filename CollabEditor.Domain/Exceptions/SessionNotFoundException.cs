using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to access a session that doesn't exist.
/// </summary>
public sealed class SessionNotFoundException(SessionId sessionId) : DomainException(
    $"Session with ID '{sessionId}' was not found.",
    "SESSION_NOT_FOUND")
{
    public SessionId SessionId { get; } = sessionId;
}