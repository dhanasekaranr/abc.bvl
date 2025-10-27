using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.ScreenDefinition.Queries;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using FluentAssertions;
using Moq;

namespace abc.bvl.AdminTool.MSTests.Application;

[TestClass]
public class GetScreenDefinitionsHandlerTests
{
    private Mock<IScreenDefinitionRepository> _mockRepository = null!;
    private GetScreenDefinitionsHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IScreenDefinitionRepository>();
        _handler = new GetScreenDefinitionsHandler(_mockRepository.Object);
    }

    [TestMethod]
    public async Task Handle_WithoutPagination_ShouldReturnAllScreenDefinitions()
    {
        // Arrange
        var expectedScreens = new List<ScreenDefnDto>
        {
            new ScreenDefnDto(1, "Screen1", 1, DateTimeOffset.UtcNow, "User1", null, null),
            new ScreenDefnDto(2, "Screen2", 1, DateTimeOffset.UtcNow, "User2", null, null)
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScreens);

        var query = new GetScreenDefinitionsQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedScreens);
        _mockRepository.Verify(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithPagination_ShouldReturnPagedScreenDefinitions()
    {
        // Arrange
        var paginationRequest = new PaginationRequest { Page = 1, PageSize = 10 };
        var expectedScreens = new List<ScreenDefnDto>
        {
            new ScreenDefnDto(1, "Screen1", 1, DateTimeOffset.UtcNow, "User1", null, null)
        };

        _mockRepository
            .Setup(r => r.GetPagedAsync(null, null, paginationRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScreens);

        var query = new GetScreenDefinitionsQuery(null, paginationRequest);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        _mockRepository.Verify(
            r => r.GetPagedAsync(null, null, paginationRequest, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithStatusFilter_ShouldReturnFilteredScreenDefinitions()
    {
        // Arrange
        byte status = 1;
        var expectedScreens = new List<ScreenDefnDto>
        {
            new ScreenDefnDto(1, "ActiveScreen", 1, DateTimeOffset.UtcNow, "User1", null, null)
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScreens);

        var query = new GetScreenDefinitionsQuery(status, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(1);
        _mockRepository.Verify(r => r.GetAllAsync(status, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScreenDefnDto>());

        var query = new GetScreenDefinitionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

[TestClass]
public class GetScreenDefinitionsCountHandlerTests
{
    private Mock<IScreenDefinitionRepository> _mockRepository = null!;
    private GetScreenDefinitionsCountHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IScreenDefinitionRepository>();
        _handler = new GetScreenDefinitionsCountHandler(_mockRepository.Object);
    }

    [TestMethod]
    public async Task Handle_ShouldReturnTotalCount()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetCountAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var query = new GetScreenDefinitionsCountQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(42);
        _mockRepository.Verify(r => r.GetCountAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithFilters_ShouldReturnFilteredCount()
    {
        // Arrange
        byte status = 1;
        string searchTerm = "test";
        
        _mockRepository
            .Setup(r => r.GetCountAsync(status, searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetScreenDefinitionsCountQuery(status, searchTerm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(5);
        _mockRepository.Verify(
            r => r.GetCountAsync(status, searchTerm, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithNoMatches_ShouldReturnZero()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetCountAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetScreenDefinitionsCountQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }
}
