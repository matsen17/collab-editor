using System.Text.Json;
using CollabEditor.API.ErrorHandling;
using CollabEditor.Utilities.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CollabEditor.API.Test.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    }

    [Fact]
    public async Task TryHandleAsync_WhenAnyException_ShouldReturnTrue()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WhenAnyException_ShouldReturn500Status()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task TryHandleAsync_WhenDevelopmentMode_ShouldIncludeExceptionDetails()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var handler = new GlobalExceptionHandler(_logger, environment);

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
    public async Task TryHandleAsync_WhenProductionMode_ShouldHideExceptionDetails()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

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
    public async Task TryHandleAsync_WhenCalled_ShouldAlwaysIncludeCorrelationId()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

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
    public async Task TryHandleAsync_WhenCalled_ShouldLogAtErrorLevel()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new System.InvalidOperationException("test");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_WhenCalled_ShouldWriteJsonResponse()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

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
    public async Task TryHandleAsync_WhenCalled_ShouldIncludeTimestamp()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var handler = new GlobalExceptionHandler(_logger, environment);

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
