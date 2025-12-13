using CollabEditor.Domain.Aggregates.EditSessionAggregate;

namespace CollabEditor.Application.Models;

public sealed record ParticipantDto
{
    public required Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public required DateTime JoinedAt { get; init; }
    
    public required DateTime LastActiveAt { get; init; }
    
    public required bool IsActive { get; init; }
}