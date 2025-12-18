using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Infrastructure.Persistence.Models;

namespace CollabEditor.Infrastructure.Persistence.Mappers;

public static class ParticipantModelMapper
{
    public static Participant ToDomain(ParticipantEntity entity)
    {
        return Participant.FromPersistence(
            entity.ParticipantId,
            entity.Name,
            entity.JoinedAt,
            entity.LastActiveAt,
            entity.IsActive);
    }

    public static ParticipantEntity ToEntity(Participant domain)
    {
        return new ParticipantEntity
        {
            ParticipantId = domain.Id.Value,
            Name = domain.Name,
            JoinedAt = domain.JoinedAt,
            LastActiveAt = domain.LastActiveAt,
            IsActive = domain.IsActive
        };
    }
}