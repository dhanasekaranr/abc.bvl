using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.MSTests.Infrastructure;

[TestClass]
public class ScreenDefinitionRepositoryTests
{
    private AdminDbContext _context = null!;
    private ScreenDefinitionRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AdminDbContext(options);
        _repository = new ScreenDefinitionRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnAllScreenDefinitions()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetAllAsync(status: 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(s => s.Status == 1).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnScreenDefinition()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Screen1");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task CreateAsync_ShouldAddNewScreenDefinition()
    {
        // Arrange
        var newScreen = new ScreenDefinition
        {
            Name = "NewScreen",
            Status = 1,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(newScreen);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("NewScreen");

        var allScreens = await _context.ScreenDefinitions.ToListAsync();
        allScreens.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldModifyExistingScreenDefinition()
    {
        // Arrange
        await SeedData();
        var existing = await _context.ScreenDefinitions.FindAsync(1L);
        existing!.Name = "UpdatedScreen";
        existing.UpdatedBy = "UpdateUser";
        existing.UpdatedAt = DateTime.UtcNow;

        // Act
        var result = await _repository.UpdateAsync(existing);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("UpdatedScreen");
        result.UpdatedBy.Should().Be("UpdateUser");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveScreenDefinition()
    {
        // Arrange
        await SeedData();

        // Act
        await _repository.DeleteAsync(1);

        // Assert
        var remaining = await _context.ScreenDefinitions.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(s => s.Id == 1);
    }

    [TestMethod]
    public async Task DeleteAsync_WithInvalidId_ShouldNotThrow()
    {
        // Arrange
        await SeedData();

        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
        var remaining = await _context.ScreenDefinitions.ToListAsync();
        remaining.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedData();
        var pagination = new PaginationRequest { Page = 1, PageSize = 2 };

        // Act
        var result = await _repository.GetPagedAsync(null, null, pagination);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetPagedAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        await SeedData();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "Screen1" };

        // Act
        var result = await _repository.GetPagedAsync(null, "Screen1", pagination);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Screen1");
    }

    [TestMethod]
    public async Task GetPagedAsync_SecondPage_ShouldReturnRemainingResults()
    {
        // Arrange
        await SeedData();
        var pagination = new PaginationRequest { Page = 2, PageSize = 2 };

        // Act
        var result = await _repository.GetPagedAsync(null, null, pagination);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetCountAsync(null, null);

        // Assert
        result.Should().Be(3);
    }

    [TestMethod]
    public async Task GetCountAsync_WithFilters_ShouldReturnFilteredCount()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetCountAsync(1, "Screen1");

        // Assert
        result.Should().Be(1);
    }

    [TestMethod]
    public async Task GetCountAsync_WithNoMatches_ShouldReturnZero()
    {
        // Arrange
        await SeedData();

        // Act
        var result = await _repository.GetCountAsync(null, "NonExistent");

        // Assert
        result.Should().Be(0);
    }

    private async Task SeedData()
    {
        var screens = new[]
        {
            new ScreenDefinition { Id = 1, Name = "Screen1", Status = 1, CreatedBy = "User1", CreatedAt = DateTime.UtcNow },
            new ScreenDefinition { Id = 2, Name = "Screen2", Status = 1, CreatedBy = "User2", CreatedAt = DateTime.UtcNow },
            new ScreenDefinition { Id = 3, Name = "Screen3", Status = 0, CreatedBy = "User3", CreatedAt = DateTime.UtcNow }
        };

        _context.ScreenDefinitions.AddRange(screens);
        await _context.SaveChangesAsync();
    }
}
