using System.Text.Json;
using CollabEditor.API.ErrorHandling;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Utilities.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CollabEditor.API.Test.ErrorHandling;

public class DomainExceptionHandlerTests
{
    private readonly Mock<ILogger<DomainExceptionHandler>> _loggerMock;
    private readonly ErrorResponseFactory _factory;
    private readonly DomainExceptionHandler _handler;

    public DomainExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DomainExceptionHandler>>();
        _factory = new ErrorResponseFactory();
        _handler = new DomainExceptionHandler(_factory, _loggerMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_NonDomainException_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new System.InvalidOperationException("test");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_SetsCorrectStatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_WritesJsonResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var sessionId = SessionId.Create();
        var exception = new SessionNotFoundException(sessionId);

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.ErrorCode.Should().Be("SESSION_NOT_FOUND");
        errorResponse.Message.Should().Contain(sessionId.Value.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_LogsAtWarningLevel()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_IncludesCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-id";
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Metadata.Should().ContainKey("CorrelationId");
        errorResponse.Metadata!["CorrelationId"].ToString().Should().Be("test-correlation-id");
    }

    [Theory]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("SESSION_CLOSED", 400)]
    public async Task TryHandleAsync_DifferentErrorCodes_ReturnCorrectStatusCodes(
        string errorCode,
        int expectedStatusCode)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Create a mock domain exception with the specified error code
        DomainException exception = errorCode switch
        {
            "SESSION_NOT_FOUND" => new SessionNotFoundException(SessionId.Create()),
            "SESSION_CLOSED" => new SessionClosedException(SessionId.Create()),
            _ => throw new System.ArgumentException("Unknown error code")
        };

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatusCode);
    }
}
