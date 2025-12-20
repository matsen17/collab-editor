using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Messages;
using CollabEditor.Application.Models;
using CollabEditor.Domain.Services;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CollabEditor.Application.Commands.Handlers;

public class OperationCommandsHandler : 
    IRequestHandler<ApplyOperationCommand, Result<DocumentContent>>
{
    private readonly ILogger<OperationCommandsHandler> _logger;
    
    private readonly IEditSessionRepository _repository;
    private readonly IOperationalTransformer _transformer;
    private readonly IMessageBus _messageBus; 

    public OperationCommandsHandler(IEditSessionRepository repository, 
        IOperationalTransformer transformer,
        IMessageBus messageBus, 
        ILogger<OperationCommandsHandler> logger)
    {
        _repository = repository;
        _transformer = transformer;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task<Result<DocumentContent>> Handle(ApplyOperationCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        
        if (session is null)
        {
            return Result.Fail("Session not found");
        }

        try
        {
            session.ApplyOperation(request.Operation, _transformer);
            await _repository.UpdateAsync(session, cancellationToken);
            
            await PublishOperationAsync(session.Id, request.Operation, cancellationToken);
            return Result.Ok(session.CurrentContent);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
    
    private async Task PublishOperationAsync(
        SessionId sessionId,
        TextOperation operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new OperationMessage
            {
                SessionId = sessionId.Value,
                Operation = new OperationDto
                {
                    Type = operation.Type.ToString().ToLowerInvariant(),
                    Position = operation.Position,
                    Text = operation.Type is OperationType.Insert ? operation.Text : null,
                    Length = operation.Type is OperationType.Delete ? operation.Length : null,
                    Version = operation.Version,
                    AuthorId = operation.AuthorId.Value
                },
                Timestamp = DateTime.UtcNow
            };

            // Publish to RabbitMQ with routing key "session.operations"
            await _messageBus.PublishAsync(
                "session.operations",
                message,
                cancellationToken);

            _logger.LogDebug(
                "Published operation to RabbitMQ for session {SessionId}",
                sessionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish operation to RabbitMQ for session {SessionId}",
                sessionId.Value);
        }
    }
}