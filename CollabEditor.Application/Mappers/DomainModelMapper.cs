using System.Net.Mime;
using CollabEditor.Application.Models;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Application.Mappers;

public static class DomainModelMapper
{
    public static ParticipantDto FromDomain(this Participant participant)
    {
        return new ParticipantDto
        {
            Id = participant.Id.Value,
            Name = participant.Name,
            JoinedAt = participant.JoinedAt,
            LastActiveAt = participant.LastActiveAt,
            IsActive = participant.IsActive
        };
    }
    
    public static OperationDto FromDomain(this TextOperation operation)
    {
        return new OperationDto
        {
            Type = operation.Type.ToString().ToLowerInvariant(),
            Position = operation.Position,
            Text = operation.Text,
            Length = operation.Length,
            Version = operation.Version,
            AuthorId = operation.AuthorId.Value
        };
    }
    
    public static TextOperation ToDomain(this OperationDto operation)
    {
        var authorId = ParticipantId.From(operation.AuthorId);
        
        return operation.Type.ToLowerInvariant() switch
        {
            "insert" => TextOperation.Insert(operation.Position, operation.Text ?? string.Empty, operation.Version, authorId),
            "delete" => TextOperation.Delete(operation.Position, operation.Length ?? 0, operation.Version, authorId),
            _ => throw new ArgumentException($"Unknown operation type: {operation.Type}")
        };
    }
    
    public static SessionDto FromDomain(this EditSession session)
    {
        return new SessionDto
        {
            Id = session.Id.Value,
            Content = session.CurrentContent.Text,
            Version = session.CurrentVersion,
            IsClosed = session.IsClosed,
            CreatedAt = session.CreatedAt,
            LastModifiedAt = session.LastModifiedAt,
            Participants = session.Participants.Select(s => s.FromDomain()).ToList()
        };
    }
}