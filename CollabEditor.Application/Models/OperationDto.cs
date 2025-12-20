namespace CollabEditor.Application.Models;

public sealed record OperationDto
{
    public required string Type { get; init; } = string.Empty;
    
    public required int Position { get; init; }
    
    public required string? Text { get; init; }
    
    public required int? Length { get; init; }
    
    public required int Version { get; init; }
    
    public required Guid AuthorId { get; init; }
}