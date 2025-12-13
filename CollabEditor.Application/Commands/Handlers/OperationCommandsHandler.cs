using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Services;
using CollabEditor.Domain.ValueObjects;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Commands.Handlers;

public class OperationCommandsHandler : 
    IRequestHandler<ApplyOperationCommand, Result<DocumentContent>>
{
    private readonly IEditSessionRepository _repository;
    private readonly IOperationalTransformer _transformer;

    public OperationCommandsHandler(IEditSessionRepository repository, IOperationalTransformer transformer)
    {
        _repository = repository;
        _transformer = transformer;
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
            return Result.Ok(session.CurrentContent);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}