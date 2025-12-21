using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.Persistence;
using CollabEditor.Infrastructure.Persistence.Mappers;
using CollabEditor.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Infrastructure.Repositories;

public sealed class SqlEditSessionRepository : RepositoryBase<EditSessionEntity>, IEditSessionRepository
{
    private readonly ILogger<SqlEditSessionRepository> _logger;

    public SqlEditSessionRepository(IDbContextFactory<CollabEditorDbContext> contextFactory, ILogger<SqlEditSessionRepository> logger) : base(contextFactory)
    {
        _logger = logger;
    }


    public async Task<EditSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await QueryAsync(query => 
            query
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId.Value, cancellationToken));

        if (entity is null)
        {
            _logger.LogDebug("Session {SessionId} not found in database", sessionId);
            return null;
        }

        _logger.LogDebug("Retrieved session {SessionId} from database", sessionId);
        return EditSessionModelMapper.ToDomain(entity);
    }

    public async Task<IEnumerable<EditSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await QueryAsync(query =>
            query.Include(s => s.Participants).ToListAsync(cancellationToken));
        
        return entities.Select(EditSessionModelMapper.ToDomain);
    }

    public async Task AddAsync(EditSession session, CancellationToken cancellationToken = default)
    {
        var entity = EditSessionModelMapper.ToEntity(session);

        await ExecuteAsync(async dbSet =>
        {
            await dbSet.AddAsync(entity, cancellationToken);
        });

        _logger.LogInformation("Added session {SessionId} to database", session.Id);
    }

    public async Task UpdateAsync(EditSession session, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async dbSet =>
        {
            var existingEntity = await dbSet
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == session.Id.Value, cancellationToken);

            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Session {session.Id.Value} not found");
            }
            
            existingEntity.Content = session.CurrentContent.Text;
            existingEntity.Version = session.CurrentVersion;
            existingEntity.IsClosed = session.IsClosed;
            existingEntity.LastModifiedAt = DateTime.UtcNow;
            
            existingEntity.Participants.Clear();

            foreach (var participant in session.Participants)
            {
                existingEntity.Participants.Add(new ParticipantEntity
                {
                    SessionId = session.Id.Value,
                    ParticipantId = participant.Id.Value,
                    Name = participant.Name,
                    JoinedAt = participant.JoinedAt,
                    LastActiveAt = participant.LastActiveAt,
                    IsActive = participant.IsActive
                });
            }
        });

        _logger.LogDebug("Updated session {SessionId} in database", session.Id);
    }

    public async Task DeleteAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async dbSet =>
        {
            var entity = await dbSet
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value, cancellationToken);
                
            if (entity is not null)
            {
                dbSet.Remove(entity);
                _logger.LogInformation("Deleted session {SessionId} from database", sessionId);
            }
        });
    }
}