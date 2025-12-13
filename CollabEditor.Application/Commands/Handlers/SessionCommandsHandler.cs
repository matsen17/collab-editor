using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Commands.Handlers;

public sealed class SessionCommandsHandler : 
    IRequestHandler<CreateSessionCommand, Result<SessionId>>,
    IRequestHandler<JoinSessionCommand, Result>,
    IRequestHandler<LeaveSessionCommand, Result>
{
    private readonly IEditSessionRepository _repository;

    public SessionCommandsHandler(IEditSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SessionId>> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var initialContent = string.IsNullOrWhiteSpace(command.InitialContent)
                ? DocumentContent.Empty()
                : DocumentContent.From(command.InitialContent);
            var session = EditSession.Create(SessionId.Create(), initialContent);
            
            await _repository.AddAsync(session, cancellationToken);
            return Result.Ok(session.Id);
        }
        catch (Exception e)
        {
            return Result.Fail<SessionId>(e.Message);
        }
    }

    public async Task<Result> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        
        if (session is null)
        {
            return Result.Fail("Session not found");
        }

        try
        {
            session.AddParticipant(request.ParticipantId,  request.Name);
            await _repository.UpdateAsync(session, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }


    public async Task<Result> Handle(LeaveSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        
        if (session is null)
        {
            return Result.Fail("Session not found");
        }

        try
        {
            session.RemoveParticipant(request.ParticipantId);
            await _repository.UpdateAsync(session, cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}