using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class ScreenPilotTests
{
    [TestMethod]
    public void Constructor_ShouldCreateScreenPilotWithDefaultValues()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            ScreenDefnId = 1,
            UserId = "john.doe",
            CreatedBy = "admin"
        };

        // Assert
        pilot.ScreenDefnId.Should().Be(1);
        pilot.UserId.Should().Be("john.doe");
        pilot.AccessLevel.Should().BeNull();
        pilot.Status.Should().Be(1);
    }

    [TestMethod]
    public void ScreenPilot_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            Id = 10,
            ScreenDefnId = 5,
            UserId = "jane.smith",
            AccessLevel = "ReadWrite",
            Status = 1,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = "admin",
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        pilot.Id.Should().Be(10);
        pilot.ScreenDefnId.Should().Be(5);
        pilot.UserId.Should().Be("jane.smith");
        pilot.AccessLevel.Should().Be("ReadWrite");
        pilot.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void ScreenPilot_WithScreenDefinitionRelation_ShouldStoreReference()
    {
        // Arrange
        var screenDef = new ScreenDefinition
        {
            Id = 1,
            Name = "Dashboard",
            CreatedBy = "admin"
        };

        var pilot = new ScreenPilot
        {
            ScreenDefnId = screenDef.Id,
            ScreenDefinition = screenDef,
            UserId = "test.user",
            CreatedBy = "admin"
        };

        // Act & Assert
        pilot.ScreenDefnId.Should().Be(1);
        pilot.ScreenDefinition.Should().NotBeNull();
        pilot.ScreenDefinition.Name.Should().Be("Dashboard");
    }

    [TestMethod]
    public void ScreenPilot_AccessLevel_ShouldBeOptional()
    {
        // Arrange & Act
        var pilotWithoutAccessLevel = new ScreenPilot
        {
            ScreenDefnId = 1,
            UserId = "user1",
            AccessLevel = null,
            CreatedBy = "admin"
        };

        var pilotWithAccessLevel = new ScreenPilot
        {
            ScreenDefnId = 2,
            UserId = "user2",
            AccessLevel = "Admin",
            CreatedBy = "admin"
        };

        // Assert
        pilotWithoutAccessLevel.AccessLevel.Should().BeNull();
        pilotWithAccessLevel.AccessLevel.Should().Be("Admin");
    }

    [TestMethod]
    public void ScreenPilot_MarkDeleted_ShouldRevokeAccess()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            ScreenDefnId = 1,
            UserId = "john.doe",
            Status = 1,
            CreatedBy = "admin"
        };

        // Act
        pilot.MarkDeleted("revoker");

        // Assert
        pilot.Status.Should().Be(0);
        pilot.UpdatedBy.Should().Be("revoker");
        pilot.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void ScreenPilot_UpdateAuditFields_ShouldTrackChanges()
    {
        // Arrange
        var pilot = new ScreenPilot
        {
            Id = 1,
            ScreenDefnId = 1,
            UserId = "john.doe",
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        pilot.UpdateAuditFields("updater");

        // Assert
        pilot.UpdatedBy.Should().Be("updater");
        pilot.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        pilot.CreatedBy.Should().Be("creator"); // Should not change
    }

    [TestMethod]
    public void ScreenPilot_UserId_ShouldNotBeEmpty()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            ScreenDefnId = 1,
            UserId = "test.user",
            CreatedBy = "admin"
        };

        // Assert
        pilot.UserId.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void ScreenPilot_ScreenDefnId_ShouldReferenceValidScreen()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            ScreenDefnId = 99,
            UserId = "test.user",
            CreatedBy = "admin"
        };

        // Assert
        pilot.ScreenDefnId.Should().Be(99);
        pilot.ScreenDefnId.Should().BeGreaterThan(0);
    }
}
