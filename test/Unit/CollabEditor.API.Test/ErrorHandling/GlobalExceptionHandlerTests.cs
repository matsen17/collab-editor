using System.Text.Json;
using CollabEditor.API.ErrorHandling;
using CollabEditor.Utilities.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CollabEditor.API.Test.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly ErrorResponseFactory _factory;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _factory = new ErrorResponseFactory();
    }

    [Fact]
    public async Task TryHandleAsync_AnyException_ReturnsTrue()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_AnyException_Returns500Status()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_DevelopmentMode_IncludesExceptionDetails()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test error message");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Be("test error message");
        errorResponse.Metadata.Should().ContainKey("ExceptionType");
        errorResponse.Metadata!["ExceptionType"].ToString().Should().Be("InvalidOperationException");
        errorResponse.Metadata.Should().ContainKey("StackTrace");
    }

    [Fact]
    public async Task TryHandleAsync_ProductionMode_HidesExceptionDetails()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("sensitive error details");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Be("An error occurred while processing your request");
        errorResponse.Message.Should().NotContain("sensitive error details");
        errorResponse.Metadata.Should().NotContainKey("ExceptionType");
        errorResponse.Metadata.Should().NotContainKey("StackTrace");
    }

    [Fact]
    public async Task TryHandleAsync_AlwaysIncludesCorrelationId()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-123";
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Metadata.Should().ContainKey("CorrelationId");
        errorResponse.Metadata!["CorrelationId"].ToString().Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task TryHandleAsync_LogsAtErrorLevel()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_WritesJsonResponse()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.ErrorCode.Should().Be("UNHANDLED_ERROR");
    }

    [Fact]
    public async Task TryHandleAsync_IncludesTimestamp()
    {
        // Arrange
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var handler = new GlobalExceptionHandler(_factory, _loggerMock.Object, environmentMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        var before = DateTimeOffset.UtcNow;
        await handler.TryHandleAsync(context, exception, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, options);
        errorResponse.Should().NotBeNull();
        errorResponse!.Metadata.Should().ContainKey("Timestamp");

        // The Metadata["Timestamp"] is a JsonElement when deserialized
        var timestampElement = (JsonElement)errorResponse.Metadata!["Timestamp"];
        var timestamp = timestampElement.GetDateTimeOffset();
        timestamp.Should().BeOnOrAfter(before.AddSeconds(-1));
        timestamp.Should().BeOnOrBefore(after.AddSeconds(1));
    }
}
