namespace CollabEditor.Application.Models;

public sealed record SessionDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsClosed { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModifiedAt { get; init; }
    public List<ParticipantDto> Participants { get; init; } = [];
}