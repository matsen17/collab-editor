using CollabEditor.Application.Commands;
using CollabEditor.Application.Queries;
using CollabEditor.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CollabEditor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
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
        
        return result.IsSuccess
            ? Ok(new { sessionId = result.Value.Value })
            : BadRequest(result.Errors);
    }
    
    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var query = new GetSessionQuery
        {
            SessionId = SessionId.From(sessionId)
        };
        var result = await _mediator.Send(query);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : result.HasError(e => e.Message == "SESSION_NOT_FOUND")
                ? NotFound(new { error = result.Errors.First().Message })
                : BadRequest(new { errors = result.Errors });
    }
    
    /// <summary>
    /// Participant joins a session.
    /// </summary>
    [HttpPost("{sessionId:guid}/join")]
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
        
        return result.IsSuccess
            ? Ok(new { message = "Joined successfully" })
            : result.HasError(s => s.Message == "SESSION_NOT_FOUND")
                ? NotFound(new { error = result.Errors.First().Message })
                : BadRequest(new { errors = result.Errors });
    }
    
    /// <summary>
    /// Participant leaves a session.
    /// </summary>
    [HttpPost("{sessionId:guid}/leave")]
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
        
        return result.IsSuccess
            ? Ok(new { message = "Left successfully" })
            : result.HasError(s => s.Message == "SESSION_NOT_FOUND")
                ? NotFound(new { error = result.Errors.First().Message })
                : BadRequest(new { errors = result.Errors });
    }
}

public record CreateSessionRequest(string? InitialContent);
public record JoinSessionRequest(Guid ParticipantId, string Name);
public record LeaveSessionRequest(Guid ParticipantId);