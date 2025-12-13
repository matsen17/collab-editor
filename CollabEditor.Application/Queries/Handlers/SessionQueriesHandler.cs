using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Mappers;
using CollabEditor.Application.Models;
using FluentResults;
using MediatR;

namespace CollabEditor.Application.Queries.Handlers;

public sealed class SessionQueriesHandler : IRequestHandler<GetSessionQuery, Result<SessionDto>>
{
    private readonly IEditSessionRepository _repository;
    
    public SessionQueriesHandler(IEditSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SessionDto>> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        return session is null 
            ? Result.Fail("No session found") 
            : Result.Ok(session.FromDomain());
    }
}