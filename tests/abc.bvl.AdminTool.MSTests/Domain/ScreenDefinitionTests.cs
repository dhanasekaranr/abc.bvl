using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class ScreenDefinitionTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var screenDefn = new ScreenDefinition
        {
            ScreenGk = 12345,
            ScreenName = "OrdersManagement",
            StatusId = 1,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 1001,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 1002
        };

        // Assert
        screenDefn.ScreenGk.Should().Be(12345);
        screenDefn.ScreenName.Should().Be("OrdersManagement");
        screenDefn.StatusId.Should().Be(1);
        screenDefn.CreatedBy.Should().Be(1001);
        screenDefn.UpdatedBy.Should().Be(1002);
    }

    [TestMethod]
    public void ScreenDefinition_StatusId_ShouldSupportActiveAndInactive()
    {
        // Arrange & Act
        var activeScreen = new ScreenDefinition { StatusId = 1 };
        var inactiveScreen = new ScreenDefinition { StatusId = 0 };

        // Assert
        activeScreen.StatusId.Should().Be(1);
        inactiveScreen.StatusId.Should().Be(0);
    }

    [TestMethod]
    public void ScreenDefinition_ShouldHaveEmptyPilotCollection()
    {
        // Arrange & Act
        var screenDefn = new ScreenDefinition();

        // Assert
        screenDefn.ScreenPilots.Should().NotBeNull();
        screenDefn.ScreenPilots.Should().BeEmpty();
    }
}