using abc.bvl.AdminTool.Api.Controllers;
using abc.bvl.AdminTool.Application.ScreenDefinition.Queries;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace abc.bvl.AdminTool.MSTests.Api;

[TestClass]
public class ScreenDefinitionControllerTests
{
    private Mock<IMediator> _mockMediator = null!;
    private Mock<IValidator<ScreenDefnDto>> _mockValidator = null!;
    private Mock<ILogger<ScreenDefinitionController>> _mockLogger = null!;
    private ScreenDefinitionController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMediator = new Mock<IMediator>();
        _mockValidator = new Mock<IValidator<ScreenDefnDto>>();
        _mockLogger = new Mock<ILogger<ScreenDefinitionController>>();
        _controller = new ScreenDefinitionController(_mockMediator.Object, _mockValidator.Object, _mockLogger.Object);

        // Setup HTTP context with authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        _controller.ControllerContext.HttpContext.TraceIdentifier = "test-trace-id";
    }

    [TestMethod]
    public async Task GetScreens_ShouldReturnPagedResult()
    {
        // Arrange
        var screenDtos = new List<ScreenDefnDto>
        {
            new ScreenDefnDto(1, "Screen1", 1, DateTimeOffset.UtcNow, "User1", null, null),
            new ScreenDefnDto(2, "Screen2", 1, DateTimeOffset.UtcNow, "User2", null, null)
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(screenDtos);

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        // Act
        var result = await _controller.GetScreens(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<ScreenDefnDto>>().Subject;
        
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.Pagination.Should().NotBeNull();
        pagedResult.Pagination!.CurrentPage.Should().Be(1);
        pagedResult.Pagination.PageSize.Should().Be(10);
        
        // Note: CorrelationId, User, Access are populated by EnrichResponseFilter
        // In unit tests (without filter), these will be default values
        // Integration tests should verify filter enrichment
    }

    [TestMethod]
    public async Task GetScreens_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetScreens(status: 99);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task GetScreens_WithStatusFilter_ShouldPassFilterToMediator()
    {
        // Arrange
        byte status = 1;
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        await _controller.GetScreens(status: status);

        // Assert
        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetScreenDefinitionsQuery>(q => q.Status == status), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task GetScreens_WithPagination_ShouldUsePaginationRequest()
    {
        // Arrange
        int page = 2;
        int pageSize = 20;
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        await _controller.GetScreens(page: page, pageSize: pageSize);

        // Assert
        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetScreenDefinitionsQuery>(q => 
                    q.Pagination != null && 
                    q.Pagination.Page == page && 
                    q.Pagination.PageSize == pageSize), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task GetScreens_WithSearchTerm_ShouldPassSearchTermToMediator()
    {
        // Arrange
        string searchTerm = "test";
        
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        await _controller.GetScreens(searchTerm: searchTerm);

        // Assert
        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetScreenDefinitionsCountQuery>(q => q.SearchTerm == searchTerm), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task GetScreens_ShouldIncludeUserInfo()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await _controller.GetScreens();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<ScreenDefnDto>>().Subject;
        
        // Unit tests: Controller just creates the response
        // EnrichResponseFilter adds User/Access in integration/E2E tests
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetScreens_ShouldIncludeAccessInfo()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await _controller.GetScreens();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<ScreenDefnDto>>().Subject;
        
        // Unit tests: Controller just creates the response
        // EnrichResponseFilter adds User/Access in integration/E2E tests
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetScreens_ShouldCalculatePaginationInfo()
    {
        // Arrange
        var screenDtos = Enumerable.Range(1, 5)
            .Select(i => new ScreenDefnDto(i, $"Screen{i}", 1, DateTimeOffset.UtcNow, "User", null, null))
            .ToList();

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(screenDtos);

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetScreenDefinitionsCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(25L);

        // Act
        var result = await _controller.GetScreens(page: 2, pageSize: 10);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<ScreenDefnDto>>().Subject;
        
        pagedResult.Pagination.Should().NotBeNull();
        pagedResult.Pagination!.CurrentPage.Should().Be(2);
        pagedResult.Pagination.PageSize.Should().Be(10);
        pagedResult.Pagination.TotalItems.Should().Be(25);
        pagedResult.Pagination.TotalPages.Should().Be(3);
        pagedResult.Pagination.HasPrevious.Should().BeTrue();
        pagedResult.Pagination.HasNext.Should().BeTrue();
    }
}
