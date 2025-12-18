namespace CollabEditor.Infrastructure.Persistence.Models;

public class EditSessionEntity
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    public List<ParticipantEntity> Participants { get; set; } = [];
}