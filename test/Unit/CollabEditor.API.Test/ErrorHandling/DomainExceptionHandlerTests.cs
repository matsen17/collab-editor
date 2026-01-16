using System.Text.Json;
using CollabEditor.API.ErrorHandling;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Utilities.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CollabEditor.API.Test.ErrorHandling;

public class DomainExceptionHandlerTests
{
    private readonly ILogger<DomainExceptionHandler> _logger;
    
    private readonly DomainExceptionHandler _sut;

    public DomainExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<DomainExceptionHandler>>();
        _sut = new DomainExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_WhenDomainException_ShouldReturnTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        var result = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WhenNonDomainException_ShouldReturnFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new System.InvalidOperationException("test");

        // Act
        var result = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_WhenDomainException_ShouldSetCorrectStatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task TryHandleAsync_WhenDomainException_ShouldWriteJsonResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var sessionId = SessionId.Create();
        var exception = new SessionNotFoundException(sessionId);

        // Act
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

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
    public async Task TryHandleAsync_WhenDomainException_ShouldLogAtWarningLevel()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_WhenDomainException_ShouldIncludeCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-id";
        var exception = new SessionNotFoundException(SessionId.Create());

        // Act
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

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
    public async Task TryHandleAsync_WhenDifferentErrorCodes_ShouldReturnCorrectStatusCodes(
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
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatusCode);
    }
}
