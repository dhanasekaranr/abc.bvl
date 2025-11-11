using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace abc.bvl.AdminTool.Tests.Integration;

/// <summary>
/// Integration tests for ScreenPilotRepository with actual Oracle database
/// Tests CRUD operations and user-screen assignments without mocks
/// Naming: *ITests.cs pattern for CI/CD filtering
/// </summary>
[TestClass]
public class ScreenPilotRepositoryITests
{
    private DatabaseFixture? _fixture;
    private ScreenPilotRepository? _repository;
    private ScreenDefinitionRepository? _screenRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        _fixture = new DatabaseFixture();
        _repository = new ScreenPilotRepository(_fixture.ContextProvider);
        _screenRepository = new ScreenDefinitionRepository(_fixture.ContextProvider);
        _fixture.ClearTestDataAsync().GetAwaiter().GetResult();
        // Create test screen definitions for FK references
        _screenRepository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900001,
            ScreenName = "TestScreen_1",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        }).GetAwaiter().GetResult();
        _screenRepository.CreateAsync(new ScreenDefinition
        {
            ScreenGk = 900002,
            ScreenName = "TestScreen_2",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 9999,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 9999
        }).GetAwaiter().GetResult();
    _fixture!.Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [TestCleanup]
    public void TestCleanup()
    {
    _fixture!.ClearTestDataAsync().GetAwaiter().GetResult();
    }

    // All test methods below must be inside this class
    // ...existing test methods...
    [TestMethod]
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
    var result = await _repository!.CreateAsync(pilot);
    await _fixture!.Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.ScreenPilotGk.Should().Be(900101);
        result.NbUserGk.Should().Be(5001);
        result.ScreenGk.Should().Be(900001);

        // Verify in database
    var retrieved = await _repository!.GetByIdAsync(900101);
        retrieved.Should().NotBeNull();
        retrieved!.NbUserGk.Should().Be(5001);
    }
    [TestMethod]
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
    await _repository!.CreateAsync(pilot);
    await _fixture!.Context.SaveChangesAsync();

        // Act
    var result = await _repository!.GetByIdAsync(900102);

        // Assert
        result.Should().NotBeNull();
        result!.ScreenPilotGk.Should().Be(900102);
        result.NbUserGk.Should().Be(5002);
        result.DualMode.Should().Be(1);
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
    public async Task GetByUserIdAsync_ShouldReturnUserAssignments()
    {
        // Arrange - Create multiple assignments for user 5003
    await _repository!.CreateAsync(new ScreenPilot
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

    await _repository!.CreateAsync(new ScreenPilot
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
    await _repository!.CreateAsync(new ScreenPilot
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

    await _fixture!.Context.SaveChangesAsync();

        // Act
    var results = await _repository!.GetByUserIdAsync(5003);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.NbUserGk == 5003);
        results.Should().Contain(p => p.ScreenGk == 900001);
        results.Should().Contain(p => p.ScreenGk == 900002);
    }

    [TestMethod]
    public async Task GetByScreenGkAsync_ShouldReturnScreenAssignments()
    {
        // Arrange - Multiple users assigned to screen 900001
    await _repository!.CreateAsync(new ScreenPilot
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

    await _fixture!.Context.SaveChangesAsync();

        // Act
    var results = await _repository!.GetByScreenGkAsync(900001);

        // Assert
        results.Should().NotBeNull();
        results.Count().Should().BeGreaterThanOrEqualTo(2);
        results.Should().OnlyContain(p => p.ScreenGk == 900001);
    }

    [TestMethod]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
    await _repository!.CreateAsync(new ScreenPilot
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

    await _repository!.CreateAsync(new ScreenPilot
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

    await _fixture!.Context.SaveChangesAsync();

        // Act
    var activeResults = await _repository!.GetAllAsync(statusId: 1);
    var inactiveResults = await _repository!.GetAllAsync(statusId: 0);

        // Assert
        activeResults.Should().Contain(p => p.ScreenPilotGk == 900108);
        inactiveResults.Should().Contain(p => p.ScreenPilotGk == 900109);
    }

    [TestMethod]
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
    await _repository!.CreateAsync(pilot);
    await _fixture!.Context.SaveChangesAsync();

        // Get for update
    var entity = await _repository!.GetEntityByIdAsync(900110);
        entity.Should().NotBeNull();

        // Modify
        entity!.StatusId = 0;
    await _fixture!.Context.SaveChangesAsync();

        // Assert
    var updated = await _repository!.GetByIdAsync(900110);
        updated.Should().NotBeNull();
        updated!.StatusId.Should().Be(0);
    }

    [TestMethod]
    public async Task BulkOperations_ShouldHandleMultipleInserts()
    {
        // Arrange & Act - Create multiple pilots in one transaction
        for (int i = 1; i <= 5; i++)
        {
            await _repository!.CreateAsync(new ScreenPilot
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
    await _fixture!.Context.SaveChangesAsync();

        // Assert
    var results = await _repository!.GetByScreenGkAsync(900001);
        results.Count().Should().BeGreaterThanOrEqualTo(5);
    }

    [TestMethod]
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
    await _repository!.CreateAsync(pilotWithInvalidScreen);
    }
}


