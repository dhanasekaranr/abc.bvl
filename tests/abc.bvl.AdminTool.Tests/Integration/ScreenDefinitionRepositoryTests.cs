using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.ScreenDefinition.Queries;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace abc.bvl.AdminTool.Tests.Integration;

public class ScreenDefinitionRepositoryTests : IDisposable
{
    private readonly AdminDbContext _context;
    private readonly IScreenDefinitionRepository _repository;

    public ScreenDefinitionRepositoryTests()
    {
        // Create in-memory database for tests
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AdminDbContext(options);
        _repository = new ScreenDefinitionRepository(_context);

        // Seed test data
        SeedTestData();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllScreenDefinitions()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Act
        var result = await _repository.GetAllAsync(status: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Active Screen", result.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectScreenDefinition()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Active Screen", result.Name);
        Assert.Equal((byte)1, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    private void SeedTestData()
    {
        var screenDefinitions = new[]
        {
            new abc.bvl.AdminTool.Domain.Entities.ScreenDefinition
            {
                Id = 1,
                Name = "Active Screen",
                Status = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "test",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new abc.bvl.AdminTool.Domain.Entities.ScreenDefinition
            {
                Id = 2,
                Name = "Inactive Screen",
                Status = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "test",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            }
        };

        _context.ScreenDefinitions.AddRange(screenDefinitions);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}