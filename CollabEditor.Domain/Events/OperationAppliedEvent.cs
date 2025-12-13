using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Events;

public class OperationAppliedEvent
{
    public required SessionId SessionId { get; init; }
    
    public required TextOperation Operation { get; init; }
    
    public required DocumentContent ResultingContent { get; init; }
}