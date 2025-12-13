using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Commands;

public sealed record ApplyOperationCommand : IRequest<Result<DocumentContent>>
{
    public required SessionId SessionId { get; init; }
    
    public required TextOperation Operation { get; init; }
}