using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Application.Interfaces;

public interface IEditSessionRepository
{
    Task<EditSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    
    Task AddAsync(EditSession session, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(EditSession session, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<EditSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}