using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Models;

namespace CollabEditor.Application.Messages;

public sealed class OperationMessage : IMessage
{
    public Guid SessionId { get; set; }
    
    public OperationDto Operation { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
}