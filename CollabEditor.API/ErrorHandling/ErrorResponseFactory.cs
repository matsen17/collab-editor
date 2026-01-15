using CollabEditor.API.ErrorHandling.Models;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Utilities.Results;

namespace CollabEditor.API.ErrorHandling;

public sealed class ErrorResponseFactory
{
    public static ErrorResponse CreateFromDomainException(DomainException exception, ErrorContext context)
    {
        var metadata = new Dictionary<string, object>
        {
            ["ErrorCode"] = exception.ErrorCode,
            ["CorrelationId"] = context.CorrelationId,
            ["Timestamp"] = context.Timestamp
        };

        if (context.SessionId.HasValue)
        {
            metadata["SessionId"] = context.SessionId.Value;
        }

        if (context.ParticipantId.HasValue)
        {
            metadata["ParticipantId"] = context.ParticipantId.Value;
        }

        return new ErrorResponse
        {
            Message = exception.Message,
            ErrorCode = exception.ErrorCode,
            Metadata = metadata
        };
    }
    
    public static ErrorResponse CreateFromException(
        Exception exception, 
        ErrorContext context, 
        bool shouldIncludeDetails = false)
    {
        var metadata = new Dictionary<string, object>
        {
            ["ErrorCode"] = "UNHANDLED_ERROR",
            ["CorrelationId"] = context.CorrelationId,
            ["Timestamp"] = context.Timestamp
        };

        if (shouldIncludeDetails)
        {
            metadata["ExceptionType"] = exception.GetType().Name;
            metadata["StackTrace"] = exception.StackTrace ?? "No stack trace available";

            if (exception.InnerException is not null)
            {
                metadata["InnerException"] = exception.InnerException.Message;
            }
        }

        if (context.SessionId.HasValue)
        {
            metadata["SessionId"] = context.SessionId.Value;
        }

        if (context.ParticipantId.HasValue)
        {
            metadata["ParticipantId"] = context.ParticipantId.Value;
        }

        var message = shouldIncludeDetails
            ? exception.Message
            : "An error occurred while processing your request";

        return new ErrorResponse
        {
            Message = message,
            ErrorCode = "UNHANDLED_ERROR",
            Metadata = metadata
        };
    }
    
    public static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            // Not Found (404)
            "SESSION_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PARTICIPANT_NOT_FOUND" => StatusCodes.Status404NotFound,

            // Conflict (409)
            "PARTICIPANT_ALREADY_JOINED" => StatusCodes.Status409Conflict,
            "SESSION_ALREADY_EXISTS" => StatusCodes.Status409Conflict,

            // Bad Request (400)
            "SESSION_CLOSED" => StatusCodes.Status400BadRequest,
            "INVALID_OPERATION" => StatusCodes.Status400BadRequest,
            "PARTICIPANT_NOT_IN_SESSION" => StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,

            // Internal Server Error (500)
            "REPOSITORY_ERROR" => StatusCodes.Status500InternalServerError,
            "UNHANDLED_ERROR" => StatusCodes.Status500InternalServerError,

            // Default to Bad Request for unknown error codes
            _ => StatusCodes.Status400BadRequest
        };
    }
}
