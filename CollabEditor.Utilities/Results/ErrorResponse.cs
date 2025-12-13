using FluentResults;

namespace CollabEditor.Utilities.Results;

public sealed record ErrorResponse
{
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    
    public static ErrorResponse FromError(IError error)
    {
        return new ErrorResponse
        {
            Message = error.Message,
            ErrorCode = error.Metadata.TryGetValue("ErrorCode", out var code) 
                ? code?.ToString() 
                : null,
            Metadata = error.Metadata.Count > 0
                ? error.Metadata.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value) 
                : null
        };
    }
    
    public static List<ErrorResponse> FromErrors(IEnumerable<IError> errors)
    {
        return errors.Select(FromError).ToList();
    }
}