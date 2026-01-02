using CollabEditor.Domain.Aggregates.EditSessionAggregate;

namespace CollabEditor.Application.Interfaces;

public interface ISessionWriter
{
    /// <summary>
    /// Saves the session to the repository and publishes any domain events.
    /// </summary>
    Task SaveAsync(EditSession session, CancellationToken cancellationToken = default);
}