using CollabEditor.Application.Commands;
using CollabEditor.Application.Queries;
using CollabEditor.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CollabEditor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    
    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }
        
        var command = new CreateSessionCommand
        {
            InitialContent = request.InitialContent
        };
        
        var result = await _mediator.Send(command);
        return ToActionResult(result, sessionId => 
            Created($"/api/sessions/{sessionId.Value}", new { sessionId = sessionId.Value }));
    }
    
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(Application.Models.SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var query = new GetSessionQuery
        {
            SessionId = SessionId.From(sessionId)
        };
        var result = await _mediator.Send(query);
        
        return ToActionResult(result);
    }
    
    [HttpPost("{sessionId:guid}/join")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinSession(
        Guid sessionId,
        [FromBody] JoinSessionRequest request)
    {
        var command = new JoinSessionCommand
        {
            SessionId = SessionId.From(sessionId),
            ParticipantId = ParticipantId.From(request.ParticipantId),
            Name = request.Name
        };
        
        var result = await _mediator.Send(command);
        return ToActionResult(result, () => Ok(new { message = "Joined successfully" }));
    }
    
    [HttpPost("{sessionId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveSession(
        Guid sessionId,
        [FromBody] LeaveSessionRequest request)
    {
        var command = new LeaveSessionCommand
        {
            SessionId = SessionId.From(sessionId),
            ParticipantId =  ParticipantId.From(request.ParticipantId)
        };
        
        var result = await _mediator.Send(command);
        return ToActionResult(result, NoContent);
    }
}

public record CreateSessionRequest(string? InitialContent);
public record JoinSessionRequest(Guid ParticipantId, string Name);
public record LeaveSessionRequest(Guid ParticipantId);