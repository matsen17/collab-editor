using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Application.Commands.Handlers;

public sealed class SessionCommandsHandler : 
    IRequestHandler<CreateSessionCommand, Result<SessionId>>,
    IRequestHandler<JoinSessionCommand, Result>,
    IRequestHandler<LeaveSessionCommand, Result>
{
    private readonly IEditSessionRepository _repository;
    private readonly ISessionWriter _writer;
    private readonly ILogger<SessionCommandsHandler> _logger;

    public SessionCommandsHandler(IEditSessionRepository repository, 
        ISessionWriter writer, 
        ILogger<SessionCommandsHandler> logger)
    {
        _repository = repository;
        _writer = writer;
        _logger = logger;
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating session");
            return Result.Fail<SessionId>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return Result.Fail<SessionId>("Failed to create session");
        }
    }

    public async Task<Result> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (session is null)
        {
            return Result.Fail(new Error("Session not found")
                .WithMetadata("ErrorCode", "SESSION_NOT_FOUND"));
        }

        try
        {
            session.AddParticipant(request.ParticipantId, request.Name);
            await _writer.SaveAsync(session, cancellationToken);

            _logger.LogInformation(
                "Participant {ParticipantId} joined session {SessionId}",
                request.ParticipantId,
                request.SessionId);

            return Result.Ok();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex,
                "Domain error adding participant {ParticipantId} to session {SessionId}",
                request.ParticipantId,
                request.SessionId);

            return Result.Fail(new Error(ex.Message)
                .WithMetadata("ErrorCode", ex.ErrorCode));
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }


    public async Task<Result> Handle(LeaveSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (session is null)
        {
            return Result.Fail(new Error("Session not found")
                .WithMetadata("ErrorCode", "SESSION_NOT_FOUND"));
        }

        try
        {
            session.RemoveParticipant(request.ParticipantId);
            await _writer.SaveAsync(session, cancellationToken);

            _logger.LogInformation(
                "Participant {ParticipantId} left session {SessionId}",
                request.ParticipantId,
                request.SessionId);
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}