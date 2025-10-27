using abc.bvl.AdminTool.Api.Middleware;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;

namespace abc.bvl.AdminTool.MSTests.Api.Middleware;

[TestClass]
public class GlobalExceptionMiddlewareTests
{
    private Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock = null!;
    private Mock<IWebHostEnvironment> _environmentMock = null!;
    private DefaultHttpContext _httpContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(200); // Default status
    }

    [TestMethod]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn400()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ValidationException("Validation failed");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturn400()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ArgumentException("Invalid argument");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new UnauthorizedAccessException("Access denied");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenKeyNotFoundException_ShouldReturn404()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new KeyNotFoundException("Resource not found");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenGenericException_ShouldReturn500()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Something went wrong");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var exceptionMessage = "Test exception";
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException(exceptionMessage);
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldSetContentTypeToJson()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldReturnJsonErrorResponse()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        var middleware = new GlobalExceptionMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        responseBody.Should().NotBeNullOrEmpty();
        
        var jsonDoc = JsonDocument.Parse(responseBody);
        jsonDoc.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue();
    }
}
