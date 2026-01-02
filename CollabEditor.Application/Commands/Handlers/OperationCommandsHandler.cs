using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.Services;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Application.Commands.Handlers;

public class OperationCommandsHandler : 
    IRequestHandler<ApplyOperationCommand, Result<DocumentContent>>
{
    private readonly IEditSessionRepository _repository;
    private readonly ISessionWriter _writer;
    private readonly IOperationalTransformer _transformer;
    private readonly ILogger<OperationCommandsHandler> _logger;

    public OperationCommandsHandler(IEditSessionRepository repository, 
        IOperationalTransformer transformer,
        ISessionWriter writer, 
        ILogger<OperationCommandsHandler> logger)
    {
        _repository = repository;
        _transformer = transformer;
        _writer = writer;
        _logger = logger;
    }

    public async Task<Result<DocumentContent>> Handle(ApplyOperationCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (session is null)
        {
            return Result.Fail<DocumentContent>(new Error("Session not found")
                .WithMetadata("ErrorCode", "SESSION_NOT_FOUND"));
        }

        try
        {
            session.ApplyOperation(request.Operation, _transformer);
            await _writer.SaveAsync(session, cancellationToken);

            _logger.LogInformation(
                "Applied {OperationType} operation at position {Position} in session {SessionId}",
                request.Operation.Type,
                request.Operation.Position,
                request.SessionId);

            return Result.Ok(session.CurrentContent);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex,
                "Domain error applying operation in session {SessionId}",
                request.SessionId);

            return Result.Fail<DocumentContent>(new Error(ex.Message)
                .WithMetadata("ErrorCode", ex.ErrorCode));
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}