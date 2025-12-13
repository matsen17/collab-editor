using CollabEditor.Application.Models;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Queries;

public sealed record GetSessionQuery : IRequest<Result<SessionDto>>
{
    public required SessionId SessionId { get; init; }
}