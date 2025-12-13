using System.Collections.Concurrent;
using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Infrastructure.Repositories;

public sealed class InMemoryEditSessionRepository : IEditSessionRepository
{
    private readonly ConcurrentDictionary<Guid, EditSession> _sessions = new();
    
    public Task<EditSession?> GetByIdAsync(
        SessionId sessionId, 
        CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId.Value, out var session);
        return Task.FromResult(session);
    }
    
    public Task AddAsync(
        EditSession session, 
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryAdd(session.Id.Value, session))
        {
            throw new InvalidOperationException(
                $"Session with ID {session.Id} already exists");
        }
        
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(
        EditSession session, 
        CancellationToken cancellationToken = default)
    {
        _sessions[session.Id.Value] = session;
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(
        SessionId sessionId, 
        CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId.Value, out _);
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<EditSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeSessions = _sessions.Values
            .Where(s => !s.IsClosed)
            .ToList();
        
        return Task.FromResult<IEnumerable<EditSession>>(activeSessions);
    }
    
    public Task<bool> ExistsAsync(
        SessionId sessionId, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.ContainsKey(sessionId.Value));
    }
}