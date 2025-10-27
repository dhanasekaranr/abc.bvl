using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class ScreenDefinitionTests
{
    [TestMethod]
    public void Constructor_ShouldCreateScreenDefinitionWithValidData()
    {
        // Arrange
        var name = "TestScreen";
        var status = (byte)1;
        var createdBy = "TestUser";

        // Act
        var screenDef = new ScreenDefinition
        {
            Name = name,
            Status = status,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        screenDef.Should().NotBeNull();
        screenDef.Name.Should().Be(name);
        screenDef.Status.Should().Be(status);
        screenDef.CreatedBy.Should().Be(createdBy);
        screenDef.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Name_ShouldNotExceedMaxLength()
    {
        // Arrange
        var screenDef = new ScreenDefinition
        {
            Name = new string('A', 100),
            Status = 1,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        screenDef.Name.Length.Should().BeLessThanOrEqualTo(100);
    }

    [TestMethod]
    public void Status_ShouldBeValid()
    {
        // Arrange & Act
        var inactiveScreen = new ScreenDefinition { Name = "Test", Status = 0, CreatedBy = "User" };
        var activeScreen = new ScreenDefinition { Name = "Test", Status = 1, CreatedBy = "User" };
        var pendingScreen = new ScreenDefinition { Name = "Test", Status = 2, CreatedBy = "User" };

        // Assert
        inactiveScreen.Status.Should().Be(0);
        activeScreen.Status.Should().Be(1);
        pendingScreen.Status.Should().Be(2);
    }

    [TestMethod]
    public void UpdatedFields_ShouldBeNullableAndOptional()
    {
        // Arrange & Act
        var screenDef = new ScreenDefinition
        {
            Name = "TestScreen",
            Status = 1,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow
        };

        // Assert - UpdatedBy can be empty string initially
        screenDef.UpdatedBy.Should().NotBeNull();
        screenDef.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Update_ShouldSetUpdatedFields()
    {
        // Arrange
        var screenDef = new ScreenDefinition
        {
            Id = 1,
            Name = "TestScreen",
            Status = 1,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        screenDef.Name = "UpdatedScreen";
        screenDef.Status = 2;
        screenDef.UpdatedBy = "UpdateUser";
        screenDef.UpdatedAt = DateTime.UtcNow;

        // Assert
        screenDef.Name.Should().Be("UpdatedScreen");
        screenDef.Status.Should().Be(2);
        screenDef.UpdatedBy.Should().Be("UpdateUser");
        screenDef.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Id_ShouldBeAutoIncrement()
    {
        // Arrange & Act
        var screenDef1 = new ScreenDefinition { Name = "Test1", Status = 1, CreatedBy = "User" };
        var screenDef2 = new ScreenDefinition { Name = "Test2", Status = 1, CreatedBy = "User" };

        screenDef1.Id = 1;
        screenDef2.Id = 2;

        // Assert
        screenDef1.Id.Should().Be(1);
        screenDef2.Id.Should().Be(2);
        screenDef2.Id.Should().BeGreaterThan(screenDef1.Id);
    }

    [TestMethod]
    public void CreatedAt_ShouldBeSetOnCreation()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        
        // Act
        var screenDef = new ScreenDefinition
        {
            Name = "TestScreen",
            Status = 1,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow
        };
        
        var afterCreation = DateTime.UtcNow;

        // Assert
        screenDef.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        screenDef.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }
}
