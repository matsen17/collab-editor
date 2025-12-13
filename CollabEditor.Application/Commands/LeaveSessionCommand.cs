using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Commands;

public sealed record LeaveSessionCommand : IRequest<Result>
{
    public required SessionId SessionId { get; init; }
    public required ParticipantId ParticipantId { get; init; }
}