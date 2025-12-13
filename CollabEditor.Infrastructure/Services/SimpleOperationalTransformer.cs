using CollabEditor.Domain.Services;
using CollabEditor.Domain.ValueObjects;

namespace CollabEditor.Infrastructure.Services;

public sealed class SimpleOperationalTransformer : IOperationalTransformer
{
    public TextOperation Transform(TextOperation incomingOp, TextOperation historicalOp)
    {
        // TODO: This is temp and will be implemented later
        return incomingOp;
    }

    public TextOperation TransformAgainstMultiple(TextOperation incomingOp, IEnumerable<TextOperation> historicalOps)
    {
        // TODO: This is temp and will be implemented later
        return historicalOps.Aggregate(incomingOp, Transform);
    }
}