using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Domain.Services;

public interface IOperationalTransformer
{
    TextOperation Transform(TextOperation incomingOp, TextOperation historicalOp);
    
    TextOperation TransformAgainstMultiple(
        TextOperation incomingOp, 
        IEnumerable<TextOperation> historicalOps);
}