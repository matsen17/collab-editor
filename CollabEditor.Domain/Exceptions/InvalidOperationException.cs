namespace CollabEditor.Domain.Exceptions;

/// <summary>
/// Thrown when an operation violates domain rules.
/// </summary>
public sealed class InvalidOperationException : DomainException
{
    public InvalidOperationException(string message, string errorCode = "INVALID_OPERATION") 
        : base(message, errorCode)
    {
    }
    
    public InvalidOperationException(string message, string errorCode, Exception innerException) 
        : base(message, errorCode, innerException)
    {
    }
}