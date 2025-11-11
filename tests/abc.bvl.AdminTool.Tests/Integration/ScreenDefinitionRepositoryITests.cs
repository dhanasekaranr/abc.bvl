using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace abc.bvl.AdminTool.Tests.Integration;

/// <summary>
/// Integration tests for ScreenDefinitionRepository with actual Oracle database
/// Tests CRUD operations without mocks
/// Naming: *ITests.cs pattern for CI/CD filtering
/// </summary>
 [TestClass]
 public class ScreenDefinitionRepositoryITests
{
    private DatabaseFixture? _fixture;
    private ScreenDefinitionRepository? _repository;

    public ScreenDefinitionRepositoryITests() { }

    [TestInitialize]
    public async Task TestInitialize()
    {
        _fixture = new DatabaseFixture();
    _repository = new ScreenDefinitionRepository(_fixture!.ContextProvider);
    await _fixture!.ClearTestDataAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
    await _fixture!.ClearTestDataAsync();
    }

    [TestMethod]
    public async Task CreateAsync_ShouldInsertNewScreenDefinition_WhenValidData()
    {
        // Arrange
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 900001,
            ScreenName = "IntegrationTest_Screen1",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };

        // Act
    var result = await _repository!.CreateAsync(screenDefn);
    await _fixture!.Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.ScreenGk.Should().Be(900001);
        result.ScreenName.Should().Be("IntegrationTest_Screen1");

        // Verify it's actually in the database
    var retrieved = await _repository!.GetByIdAsync(900001);
        retrieved.Should().NotBeNull();
        retrieved!.ScreenName.Should().Be("IntegrationTest_Screen1");
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnScreenDefinition_WhenExists()
    {
        // Arrange
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 900002,
            ScreenName = "IntegrationTest_Screen2",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
    await _repository!.CreateAsync(screenDefn);
    await _fixture!.Context.SaveChangesAsync();

        // Act
    var result = await _repository!.GetByIdAsync(900002);

        // Assert
        result.Should().NotBeNull();
        result!.ScreenGk.Should().Be(900002);
        result.ScreenName.Should().Be("IntegrationTest_Screen2");
        result.StatusId.Should().Be(1);
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
    var result = await _repository!.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnAllScreenDefinitions()
    {
        // Arrange
    await _repository!.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900003,
            ScreenName = "IntegrationTest_Screen3",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

    await _repository!.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900004,
            ScreenName = "IntegrationTest_Screen4",
            StatusId = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

    await _fixture!.Context.SaveChangesAsync();

        // Act
    var results = await _repository!.GetAllAsync();

        // Assert
        results.Should().NotBeNull();
        results.Should().Contain(s => s.ScreenGk == 900003);
        results.Should().Contain(s => s.ScreenGk == 900004);
    }

    [TestMethod]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await _repository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900005,
            ScreenName = "IntegrationTest_Active",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900006,
            ScreenName = "IntegrationTest_Inactive",
            StatusId = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act
    var activeResults = await _repository!.GetAllAsync(status: 1);
    var inactiveResults = await _repository!.GetAllAsync(status: 0);

        // Assert
    Assert.IsNotNull(activeResults);
    Assert.IsNotNull(inactiveResults);
    activeResults.Should().Contain(s => s.ScreenGk == 900005);
    activeResults.Should().NotContain(s => s.ScreenGk == 900006);

    inactiveResults.Should().Contain(s => s.ScreenGk == 900006);
    inactiveResults.Should().NotContain(s => s.ScreenGk == 900005);
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldModifyExistingScreenDefinition()
    {
        // Arrange
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 900007,
            ScreenName = "IntegrationTest_Original",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
    await _repository!.CreateAsync(screenDefn);
    await _fixture!.Context.SaveChangesAsync();

        // Get the entity for update
        var entity = await _repository!.GetEntityByIdAsync(900007);
        entity.Should().NotBeNull();
        if (entity != null)
        {
            // Modify
            entity.ScreenName = "IntegrationTest_Updated";
            entity.StatusId = 0;
            entity.UpdatedDt = DateTime.UtcNow;
            entity.UpdatedBy = 8888;
        }

        // Act
        if (entity != null)
        {
            var result = await _repository!.UpdateAsync(entity);
            await _fixture!.Context.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.ScreenName.Should().Be("IntegrationTest_Updated");
            result.StatusId.Should().Be(0);
            result.UpdatedBy.Should().Be(8888);

            // Verify in database
            var retrieved = await _repository!.GetByIdAsync(900007);
            Assert.IsNotNull(retrieved);
            retrieved.ScreenName.Should().Be("IntegrationTest_Updated");
            retrieved.StatusId.Should().Be(0);
        }
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveScreenDefinition()
    {
        // Arrange
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 900008,
            ScreenName = "IntegrationTest_ToDelete",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
    await _repository!.CreateAsync(screenDefn);
    await _fixture!.Context.SaveChangesAsync();

        // Verify it exists
    var beforeDelete = await _repository!.GetByIdAsync(900008);
        beforeDelete.Should().NotBeNull();

        // Act
    await _repository!.DeleteAsync(900008);
    await _fixture!.Context.SaveChangesAsync();

        // Assert
    var afterDelete = await _repository!.GetByIdAsync(900008);
        afterDelete.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnPaginatedResults()
    {
        // Arrange - Create 5 test records
        for (int i = 1; i <= 5; i++)
        {
            await _repository!.CreateAsync(new ScreenDefinition
            {
                ScreenGk = 900010 + i,
                ScreenName = $"IntegrationTest_Paged_{i}",
                StatusId = 1,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }
    await _fixture!.Context.SaveChangesAsync();

        // Act - Get page 1 with 2 items
    var page1 = await _repository!.GetPagedAsync(
            status: 1,
            searchTerm: "IntegrationTest_Paged",
            pagination: new abc.bvl.AdminTool.Contracts.Common.PaginationRequest { Page = 1, PageSize = 2 });

        // Assert
        page1.Should().NotBeNull();
        page1.Count().Should().BeLessThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (int i = 1; i <= 3; i++)
        {
            await _repository!.CreateAsync(new ScreenDefinition
            {
                ScreenGk = 900020 + i,
                ScreenName = $"IntegrationTest_Count_{i}",
                StatusId = 1,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }
    await _fixture!.Context.SaveChangesAsync();

        // Act
    var count = await _repository!.GetCountAsync(status: 1, searchTerm: "IntegrationTest_Count");

        // Assert
        count.Should().BeGreaterThanOrEqualTo(3);
    }

    [TestMethod]
    public async Task ConcurrentUpdates_ShouldHandleMultipleUpdates()
    {
        // Arrange
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 900030,
            ScreenName = "IntegrationTest_Concurrent",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
    await _repository!.CreateAsync(screenDefn);
    await _fixture!.Context.SaveChangesAsync();

        // Act - Get entity twice (simulating two concurrent users)
    var entity1 = await _repository!.GetEntityByIdAsync(900030);
    var entity2 = await _repository!.GetEntityByIdAsync(900030);

        entity1!.ScreenName = "Updated_By_User1";
        entity2!.ScreenName = "Updated_By_User2";

        // First update should succeed
    await _repository!.UpdateAsync(entity1);
    await _fixture!.Context.SaveChangesAsync();

        // Second update should also succeed (last write wins)
    await _repository!.UpdateAsync(entity2);
    await _fixture!.Context.SaveChangesAsync();

        // Assert
    var final = await _repository!.GetByIdAsync(900030);
        final!.ScreenName.Should().Be("Updated_By_User2");
    }

    [TestMethod]
    public async Task BulkInsert_ShouldHandleMultipleRecords()
    {
        // Arrange & Act - Create 10 records in one transaction
        for (int i = 1; i <= 10; i++)
        {
            await _repository!.CreateAsync(new ScreenDefinition
            {
                ScreenGk = 900040 + i,
                ScreenName = $"IntegrationTest_Bulk_{i}",
                StatusId = 1,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }
    await _fixture!.Context.SaveChangesAsync();

        // Assert
    var results = await _repository!.GetAllAsync(status: 1);
        var bulkRecords = results.Where(s => s.ScreenGk >= 900041 && s.ScreenGk <= 900050);
        bulkRecords.Should().HaveCount(10);
    }
}
