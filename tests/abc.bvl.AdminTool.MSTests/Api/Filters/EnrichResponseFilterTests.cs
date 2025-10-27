using abc.bvl.AdminTool.Api.Filters;
using abc.bvl.AdminTool.Contracts.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace abc.bvl.AdminTool.MSTests.Api.Filters;

[TestClass]
public class EnrichResponseFilterTests
{
    private Mock<ILogger<EnrichResponseFilter>> _loggerMock = null!;
    private EnrichResponseFilter _filter = null!;
    private DefaultHttpContext _httpContext = null!;
    private ResultExecutingContext _executingContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EnrichResponseFilter>>();
        _filter = new EnrichResponseFilter(_loggerMock.Object);
        _httpContext = new DefaultHttpContext();
        
        var actionContext = new ActionContext(
            _httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        );
        
        _executingContext = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new OkObjectResult(null),
            new object()
        );
    }

    [TestMethod]
    public void OnResultExecuting_WhenNotOkResult_ShouldNotEnrich()
    {
        // Arrange
        _executingContext.Result = new NotFoundResult();

        // Act
        _filter.OnResultExecuting(_executingContext);

        // Assert
        _executingContext.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public void OnResultExecuting_WhenOkResultWithNullValue_ShouldNotThrow()
    {
        // Arrange
        _executingContext.Result = new OkObjectResult(null);

        // Act
        var act = () => _filter.OnResultExecuting(_executingContext);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void OnResultExecuting_WhenNotBasePageDto_ShouldNotModify()
    {
        // Arrange
        var simpleObject = new { Name = "Test" };
        _executingContext.Result = new OkObjectResult(simpleObject);

        // Act
        _filter.OnResultExecuting(_executingContext);

        // Assert
        var okResult = _executingContext.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(simpleObject);
    }

    [TestMethod]
    public void OnResultExecuting_WhenBasePageDto_ShouldEnrichWithUserInfo()
    {
        // Arrange
        var dto = new SingleResult<string>("Test Data");
        _executingContext.Result = new OkObjectResult(dto);
        
        // Add user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Email, "john@example.com")
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        _filter.OnResultExecuting(_executingContext);

        // Assert
        var okResult = _executingContext.Result.Should().BeOfType<OkObjectResult>().Subject;
        var enrichedDto = okResult.Value.Should().BeOfType<SingleResult<string>>().Subject;
        enrichedDto.User.Should().NotBeNull();
        enrichedDto.User!.UserId.Should().Be("user123");
        enrichedDto.User.DisplayName.Should().Be("John Doe");
        enrichedDto.User.Email.Should().Be("john@example.com");
    }

    [TestMethod]
    public void OnResultExecuting_WhenBasePageDto_ShouldEnrichWithAccessInfo()
    {
        // Arrange
        var dto = new SingleResult<string>("Test Data");
        _executingContext.Result = new OkObjectResult(dto);
        
        // Add user claims with roles
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Email, "john@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User")
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        _filter.OnResultExecuting(_executingContext);

        // Assert
        var okResult = _executingContext.Result.Should().BeOfType<OkObjectResult>().Subject;
        var enrichedDto = okResult.Value.Should().BeOfType<SingleResult<string>>().Subject;
        enrichedDto.Access.Should().NotBeNull();
        enrichedDto.Access!.Roles.Should().Contain("Admin");
        enrichedDto.Access.Roles.Should().Contain("User");
    }

    [TestMethod]
    public void OnResultExecuting_WhenBasePageDto_ShouldSetCorrelationId()
    {
        // Arrange
        var dto = new SingleResult<string>("Test Data");
        _executingContext.Result = new OkObjectResult(dto);
        _httpContext.TraceIdentifier = "test-correlation-id";

        // Act
        _filter.OnResultExecuting(_executingContext);

        // Assert
        var okResult = _executingContext.Result.Should().BeOfType<OkObjectResult>().Subject;
        var enrichedDto = okResult.Value.Should().BeOfType<SingleResult<string>>().Subject;
        enrichedDto.CorrelationId.Should().Be("test-correlation-id");
    }

    [TestMethod]
    public void OnResultExecuting_WhenBasePageDto_ShouldSetServerTime()
    {
        // Arrange
        var dto = new SingleResult<string>("Test Data");
        _executingContext.Result = new OkObjectResult(dto);
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        _filter.OnResultExecuting(_executingContext);
        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        var okResult = _executingContext.Result.Should().BeOfType<OkObjectResult>().Subject;
        var enrichedDto = okResult.Value.Should().BeOfType<SingleResult<string>>().Subject;
        enrichedDto.ServerTime.Should().BeOnOrAfter(beforeTime);
        enrichedDto.ServerTime.Should().BeOnOrBefore(afterTime);
    }

    [TestMethod]
    public void OnResultExecuting_WhenEnrichmentFails_ShouldNotThrow()
    {
        // Arrange
        var dto = new SingleResult<string>("Test Data");
        _executingContext.Result = new OkObjectResult(dto);
        _httpContext.User = null!; // This might cause issues

        // Act
        var act = () => _filter.OnResultExecuting(_executingContext);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void OnResultExecuted_ShouldDoNothing()
    {
        // Arrange
        var executedContext = new ResultExecutedContext(
            new ActionContext(
                _httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            ),
            new List<IFilterMetadata>(),
            new OkResult(),
            new object()
        );

        // Act
        var act = () => _filter.OnResultExecuted(executedContext);

        // Assert
        act.Should().NotThrow();
    }
}
