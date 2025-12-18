namespace CollabEditor.Infrastructure.Persistence.Models;

public class ParticipantEntity
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid ParticipantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public bool IsActive { get; set; }
    
    public EditSessionEntity Session { get; set; } = null!;
}