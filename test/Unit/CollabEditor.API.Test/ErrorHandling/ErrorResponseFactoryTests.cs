using CollabEditor.API.ErrorHandling;
using CollabEditor.API.ErrorHandling.Models;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CollabEditor.API.Test.ErrorHandling;

public class ErrorResponseFactoryTests
{
    [Fact]
    public void CreateFromDomainException_IncludesErrorCodeAndMessage()
    {
        // Arrange
        var sessionId = SessionId.Create();
        var exception = new SessionNotFoundException(sessionId);
        var context = new ErrorContext
        {
            CorrelationId = "test-correlation-id",
            RequestPath = "/api/sessions/123",
            HttpMethod = "GET",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromDomainException(exception, context);

        // Assert
        response.Message.Should().Contain(sessionId.Value.ToString());
        response.ErrorCode.Should().Be("SESSION_NOT_FOUND");
        response.Metadata.Should().NotBeNull();
        response.Metadata!["ErrorCode"].Should().Be("SESSION_NOT_FOUND");
    }

    [Fact]
    public void CreateFromDomainException_IncludesCorrelationId()
    {
        // Arrange
        var exception = new SessionNotFoundException(SessionId.Create());
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "GET",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromDomainException(exception, context);

        // Assert
        response.Metadata.Should().ContainKey("CorrelationId");
        response.Metadata!["CorrelationId"].Should().Be("test-123");
    }

    [Fact]
    public void CreateFromDomainException_IncludesTimestamp()
    {
        // Arrange
        var exception = new SessionNotFoundException(SessionId.Create());
        var timestamp = DateTimeOffset.UtcNow;
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "GET",
            Timestamp = timestamp
        };

        // Act
        var response = ErrorResponseFactory.CreateFromDomainException(exception, context);

        // Assert
        response.Metadata.Should().ContainKey("Timestamp");
        response.Metadata!["Timestamp"].Should().Be(timestamp);
    }

    [Fact]
    public void CreateFromDomainException_IncludesSessionId_WhenAvailable()
    {
        // Arrange
        var exception = new SessionNotFoundException(SessionId.Create());
        var sessionId = Guid.NewGuid();
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "GET",
            SessionId = sessionId,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromDomainException(exception, context);

        // Assert
        response.Metadata.Should().ContainKey("SessionId");
        response.Metadata!["SessionId"].Should().Be(sessionId);
    }

    [Fact]
    public void CreateFromException_WithDetails_IncludesExceptionType()
    {
        // Arrange
        var exception = new System.InvalidOperationException("Test error");
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "POST",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromException(exception, context, shouldIncludeDetails: true);

        // Assert
        response.Metadata.Should().ContainKey("ExceptionType");
        response.Metadata!["ExceptionType"].Should().Be("InvalidOperationException");
    }

    [Fact]
    public void CreateFromException_WithDetails_IncludesStackTrace()
    {
        // Arrange
        var exception = new System.InvalidOperationException("Test error");
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "POST",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromException(exception, context, shouldIncludeDetails: true);

        // Assert
        response.Metadata.Should().ContainKey("StackTrace");
    }

    [Fact]
    public void CreateFromException_WithoutDetails_HidesExceptionDetails()
    {
        // Arrange
        var exception = new System.InvalidOperationException("Test error");
        var context = new ErrorContext
        {
            CorrelationId = "test-123",
            RequestPath = "/api/sessions",
            HttpMethod = "POST",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromException(exception, context, shouldIncludeDetails: false);

        // Assert
        response.Message.Should().Be("An error occurred while processing your request");
        response.Metadata.Should().NotContainKey("ExceptionType");
        response.Metadata.Should().NotContainKey("StackTrace");
    }

    [Fact]
    public void CreateFromException_AlwaysIncludesCorrelationId()
    {
        // Arrange
        var exception = new System.InvalidOperationException("Test error");
        var context = new ErrorContext
        {
            CorrelationId = "test-456",
            RequestPath = "/api/sessions",
            HttpMethod = "POST",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = ErrorResponseFactory.CreateFromException(exception, context, shouldIncludeDetails: false);

        // Assert
        response.Metadata.Should().ContainKey("CorrelationId");
        response.Metadata!["CorrelationId"].Should().Be("test-456");
    }

    [Theory]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("PARTICIPANT_NOT_FOUND", 404)]
    [InlineData("PARTICIPANT_ALREADY_JOINED", 409)]
    [InlineData("SESSION_ALREADY_EXISTS", 409)]
    [InlineData("SESSION_CLOSED", 400)]
    [InlineData("INVALID_OPERATION", 400)]
    [InlineData("PARTICIPANT_NOT_IN_SESSION", 400)]
    [InlineData("VALIDATION_ERROR", 400)]
    [InlineData("REPOSITORY_ERROR", 500)]
    [InlineData("UNHANDLED_ERROR", 500)]
    public void GetStatusCode_MapsErrorCodeToCorrectHttpStatus(string errorCode, int expectedStatusCode)
    {
        // Act
        var statusCode = ErrorResponseFactory.GetStatusCode(errorCode);

        // Assert
        statusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void GetStatusCode_UnknownErrorCode_Returns400()
    {
        // Act
        var statusCode = ErrorResponseFactory.GetStatusCode("UNKNOWN_ERROR");

        // Assert
        statusCode.Should().Be(400);
    }
}
