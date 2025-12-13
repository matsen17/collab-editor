using CollabEditor.Utilities.Results;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace CollabEditor.API.Controllers;

/// <summary>
/// Base controller with Result to ActionResult mapping.
/// Keeps API concerns in API layer, not in Application layer.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        return result.IsSuccess 
            ? Ok() 
            : MapErrorsToActionResult(result.Errors);
    }
    
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        return result.IsSuccess 
            ? Ok(result.Value) 
            : MapErrorsToActionResult(result.Errors);
    }

    protected IActionResult ToActionResult<T>(
        Result<T> result,
        Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess 
            ? onSuccess(result.Value) 
            : MapErrorsToActionResult(result.Errors);
    }
    
    protected IActionResult ToActionResult(
        Result result,
        Func<IActionResult> onSuccess)
    {
        return result.IsSuccess 
            ? onSuccess() 
            : MapErrorsToActionResult(result.Errors);
    }
    
    private IActionResult MapErrorsToActionResult(IEnumerable<IError> errors)
    {
        var errorsList = errors.ToList();
        var firstError = errorsList.First();
        var errorCode = GetErrorCode(firstError);
        
        return errorCode switch
        {
            "SESSION_NOT_FOUND" => NotFound(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "PARTICIPANT_NOT_FOUND" => NotFound(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "PARTICIPANT_ALREADY_JOINED" => Conflict(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "SESSION_ALREADY_EXISTS" => Conflict(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "SESSION_CLOSED" => BadRequest(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "INVALID_OPERATION" => BadRequest(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "PARTICIPANT_NOT_IN_SESSION" => BadRequest(new 
            { 
                error = firstError.Message,
                errorCode
            }),
            
            "REPOSITORY_ERROR" => StatusCode(
                StatusCodes.Status500InternalServerError,
                new 
                { 
                    error = "An error occurred while processing your request",
                    errorCode
                }),
            
            _ => BadRequest(new 
            { 
                errors = ErrorResponse.FromErrors(errorsList)
            })
        };
    }
    
    private static string? GetErrorCode(IError error)
    {
        return error.Metadata.TryGetValue("ErrorCode", out var code) 
            ? code?.ToString() 
            : null;
    }
}