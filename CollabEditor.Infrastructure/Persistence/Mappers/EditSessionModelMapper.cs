using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Infrastructure.Persistence.Models;

namespace CollabEditor.Infrastructure.Persistence.Mappers;

public static class EditSessionModelMapper
{
    public static EditSession ToDomain(EditSessionEntity entity)
    {
        var participants = entity.Participants
            .Select(ParticipantModelMapper.ToDomain)
            .ToList();

        return EditSession.FromPersistence(
            entity.Id,
            entity.Content,
            entity.Version,
            entity.IsClosed,
            entity.CreatedAt,
            entity.LastModifiedAt,
            participants);
    }
    
    public static EditSessionEntity ToEntity(EditSession domain)
    {
        return new EditSessionEntity
        {
            Id = domain.Id.Value,
            Content = domain.CurrentContent.Text,
            Version = domain.CurrentVersion,
            IsClosed = domain.IsClosed,
            CreatedAt = domain.CreatedAt,
            LastModifiedAt = domain.LastModifiedAt,
            Participants = domain.Participants
                .Select(ParticipantModelMapper.ToEntity)
                .ToList()
        };
    }
}