using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using FluentAssertions;

namespace abc.bvl.AdminTool.Tests.Integration;

/// <summary>
/// Integration tests for ScreenPilotRepository with actual Oracle database
/// Tests CRUD operations and user-screen assignments without mocks
/// Naming: *ITests.cs pattern for CI/CD filtering
/// </summary>
[Collection("Database collection")]
public class ScreenPilotRepositoryITests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ScreenPilotRepository _repository;
    private readonly ScreenDefinitionRepository _screenRepository;

    public ScreenPilotRepositoryITests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new ScreenPilotRepository(_fixture.ContextProvider);
        _screenRepository = new ScreenDefinitionRepository(_fixture.ContextProvider);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearTestDataAsync();

        // Create test screen definitions for FK references
        await _screenRepository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900001,
            ScreenName = "TestScreen_1",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _screenRepository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900002,
            ScreenName = "TestScreen_2",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.ClearTestDataAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldInsertNewScreenPilot_WhenValidData()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            ScreenPilotGk = 900101,
            NbUserGk = 5001,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };

        // Act
        var result = await _repository.CreateAsync(pilot);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.ScreenPilotGk.Should().Be(900101);
        result.NbUserGk.Should().Be(5001);
        result.ScreenGk.Should().Be(900001);

        // Verify in database
        var retrieved = await _repository.GetByIdAsync(900101);
        retrieved.Should().NotBeNull();
        retrieved!.NbUserGk.Should().Be(5001);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnScreenPilot_WhenExists()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            ScreenPilotGk = 900102,
            NbUserGk = 5002,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
        await _repository.CreateAsync(pilot);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(900102);

        // Assert
        result.Should().NotBeNull();
        result!.ScreenPilotGk.Should().Be(900102);
        result.NbUserGk.Should().Be(5002);
        result.DualMode.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserAssignments()
    {
        // Arrange - Create multiple assignments for user 5003
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900103,
            NbUserGk = 5003,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900104,
            NbUserGk = 5003,
            ScreenGk = 900002,
            StatusId = 1,
            DualMode = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        // Different user
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900105,
            NbUserGk = 5004,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByUserIdAsync(5003);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.NbUserGk == 5003);
        results.Should().Contain(p => p.ScreenGk == 900001);
        results.Should().Contain(p => p.ScreenGk == 900002);
    }

    [Fact]
    public async Task GetByScreenGkAsync_ShouldReturnScreenAssignments()
    {
        // Arrange - Multiple users assigned to screen 900001
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900106,
            NbUserGk = 5005,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900107,
            NbUserGk = 5006,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByScreenGkAsync(900001);

        // Assert
        results.Should().NotBeNull();
        results.Count().Should().BeGreaterThanOrEqualTo(2);
        results.Should().OnlyContain(p => p.ScreenGk == 900001);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900108,
            NbUserGk = 5007,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900109,
            NbUserGk = 5008,
            ScreenGk = 900001,
            StatusId = 0,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act
        var activeResults = await _repository.GetAllAsync(statusId: 1);
        var inactiveResults = await _repository.GetAllAsync(statusId: 0);

        // Assert
        activeResults.Should().Contain(p => p.ScreenPilotGk == 900108);
        inactiveResults.Should().Contain(p => p.ScreenPilotGk == 900109);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingScreenPilot()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            ScreenPilotGk = 900110,
            NbUserGk = 5009,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
        await _repository.CreateAsync(pilot);
        await _fixture.Context.SaveChangesAsync();

        // Get for update
        var entity = await _repository.GetEntityByIdAsync(900110);
        entity.Should().NotBeNull();

        // Modify
        entity!.StatusId = 0;
        entity.DualMode = 1;
        entity.UpdatedDt = DateTime.UtcNow;
        entity.UpdatedBy = 8888;

        // Act
        var result = await _repository.UpdateAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.StatusId.Should().Be(0);
        result.DualMode.Should().Be(1);
        result.UpdatedBy.Should().Be(8888);

        // Verify in database
        var retrieved = await _repository.GetByIdAsync(900110);
        retrieved!.StatusId.Should().Be(0);
        retrieved.DualMode.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveScreenPilot()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            ScreenPilotGk = 900111,
            NbUserGk = 5010,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };
        await _repository.CreateAsync(pilot);
        await _fixture.Context.SaveChangesAsync();

        // Verify exists
        var beforeDelete = await _repository.GetByIdAsync(900111);
        beforeDelete.Should().NotBeNull();

        // Act
        await _repository.DeleteAsync(900111);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var afterDelete = await _repository.GetByIdAsync(900111);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task GetAllQueryable_ShouldAllowLinqQueries()
    {
        // Arrange
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900112,
            NbUserGk = 5011,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900113,
            NbUserGk = 5012,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act
        var queryable = _repository.GetAllQueryable(statusId: 1);
        var dualModeEnabled = queryable.Where(p => p.DualMode == 1).ToList();

        // Assert
        queryable.Should().NotBeNull();
        dualModeEnabled.Should().Contain(p => p.ScreenPilotGk == 900113);
        dualModeEnabled.Should().NotContain(p => p.ScreenPilotGk == 900112);
    }

    [Fact]
    public async Task BulkOperations_ShouldHandleMultipleInserts()
    {
        // Arrange & Act - Create multiple pilots in one transaction
        for (int i = 1; i <= 5; i++)
        {
            await _repository.CreateAsync(new ScreenPilot
            {
                ScreenPilotGk = 900120 + i,
                NbUserGk = 5020 + i,
                ScreenGk = 900001,
                StatusId = 1,
                DualMode = 0,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var results = await _repository.GetByScreenGkAsync(900001);
        results.Count().Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task UserScreenAssignment_ShouldEnforceForeignKeys()
    {
        // Arrange - Try to create pilot with non-existent screen
        var pilotWithInvalidScreen = new ScreenPilot
        {
            ScreenPilotGk = 900130,
            NbUserGk = 5030,
            ScreenGk = 999999, // Non-existent screen
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };

        // Act & Assert
        await _repository.CreateAsync(pilotWithInvalidScreen);

        // Should throw when saving due to FK constraint
        var act = async () => await _fixture.Context.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>(); // FK violation
    }

    [Fact]
    public async Task MultipleUsersOnScreen_ShouldReturnCorrectCounts()
    {
        // Arrange - Assign 3 users to screen 900001
        for (int i = 1; i <= 3; i++)
        {
            await _repository.CreateAsync(new ScreenPilot
            {
                ScreenPilotGk = 900140 + i,
                NbUserGk = 5040 + i,
                ScreenGk = 900001,
                StatusId = 1,
                DualMode = 0,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }

        // Assign 2 users to screen 900002
        for (int i = 1; i <= 2; i++)
        {
            await _repository.CreateAsync(new ScreenPilot
            {
                ScreenPilotGk = 900150 + i,
                NbUserGk = 5050 + i,
                ScreenGk = 900002,
                StatusId = 1,
                DualMode = 0,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 9999,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 9999
            });
        }

        await _fixture.Context.SaveChangesAsync();

        // Act
        var screen1Users = await _repository.GetByScreenGkAsync(900001);
        var screen2Users = await _repository.GetByScreenGkAsync(900002);

        // Assert
        screen1Users.Count().Should().BeGreaterThanOrEqualTo(3);
        screen2Users.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task StatusFilter_ShouldOnlyReturnActiveRecords_InGetByUserIdAsync()
    {
        // Arrange - Create active and inactive assignments for same user
        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900160,
            NbUserGk = 5060,
            ScreenGk = 900001,
            StatusId = 1, // Active
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _repository.CreateAsync(new ScreenPilot
        {
            ScreenPilotGk = 900161,
            NbUserGk = 5060,
            ScreenGk = 900002,
            StatusId = 0, // Inactive
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        });

        await _fixture.Context.SaveChangesAsync();

        // Act - GetByUserIdAsync should only return active (statusId=1)
        var results = await _repository.GetByUserIdAsync(5060);

        // Assert
        results.Should().HaveCount(1);
        results.Should().OnlyContain(p => p.StatusId == 1);
        results.First().ScreenPilotGk.Should().Be(900160);
    }

    [Fact]
    public async Task DualModeFlag_ShouldBeStoredAndRetrievedCorrectly()
    {
        // Arrange & Act
        var pilotWithDualMode = new ScreenPilot
        {
            ScreenPilotGk = 900170,
            NbUserGk = 5070,
            ScreenGk = 900001,
            StatusId = 1,
            DualMode = 1, // Enabled
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        };

        await _repository.CreateAsync(pilotWithDualMode);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(900170);
        retrieved.Should().NotBeNull();
        retrieved!.DualMode.Should().Be(1);
    }
}
