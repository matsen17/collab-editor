namespace CollabEditor.Domain.ValueObjects;

/// <summary>
/// Represents the text content of a collaborative document.
/// Immutable - operations return new instances.
/// </summary>
public sealed record DocumentContent
{
    private const int MaxContentLength = 1_000_000; // 1MB character limit
    
    public string Text { get; }
    public int Length => Text.Length;
    
    private DocumentContent(string text)
    {
        Text = text;
    }
    
    /// <summary>
    /// Creates an empty document.
    /// </summary>
    public static DocumentContent Empty()
    {
        return new DocumentContent(string.Empty);
    }
    
    /// <summary>
    /// Creates document content from text with validation.
    /// </summary>
    public static DocumentContent From(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        
        if (text.Length > MaxContentLength)
            throw new ArgumentException(
                $"Content exceeds maximum length of {MaxContentLength} characters", 
                nameof(text));
        
        return new DocumentContent(text);
    }
    
    /// <summary>
    /// Returns a new DocumentContent with text inserted at the specified position.
    /// </summary>
    public DocumentContent InsertAt(int position, string text)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(
                nameof(position), 
                $"Position must be between 0 and {Length}");
        
        if (string.IsNullOrEmpty(text))
            return this; // No change
        
        var newText = Text.Insert(position, text);
        return From(newText);
    }
    
    /// <summary>
    /// Returns a new DocumentContent with text deleted from the specified position.
    /// </summary>
    public DocumentContent DeleteAt(int position, int length)
    {
        if (position < 0 || position >= Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position must be between 0 and {Length - 1}");
        }
        
        if (length < 0 || position + length > Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"Length must be between 0 and {Length - position}");
        }
        
        if (length == 0)
        {
            return this; // No change
        }
        
        var newText = Text.Remove(position, length);
        return From(newText);
    }
    
    public override string ToString() => Text;
}