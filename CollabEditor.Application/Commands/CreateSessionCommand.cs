using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Commands;

public sealed record CreateSessionCommand : IRequest<Result<SessionId>>
{
    public required string? InitialContent { get; init; }
}