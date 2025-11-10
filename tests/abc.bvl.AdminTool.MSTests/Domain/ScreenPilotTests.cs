using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class ScreenPilotTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            ScreenPilotGk = 100,
            NbUserGk = 5001,
            ScreenGk = 2000,
            StatusId = 1,
            DualMode = 0,
            CreatedDt = DateTime.UtcNow,
            CreatedBy = 1001,
            UpdatedDt = DateTime.UtcNow,
            UpdatedBy = 1002
        };

        // Assert
        pilot.ScreenPilotGk.Should().Be(100);
        pilot.NbUserGk.Should().Be(5001);
        pilot.ScreenGk.Should().Be(2000);
        pilot.StatusId.Should().Be(1);
        pilot.DualMode.Should().Be(0);
        pilot.CreatedBy.Should().Be(1001);
        pilot.UpdatedBy.Should().Be(1002);
    }

    [TestMethod]
    public void ScreenPilot_ShouldLinkUserAndScreen()
    {
        // Arrange & Act
        var pilot = new ScreenPilot
        {
            NbUserGk = 9999,
            ScreenGk = 1111
        };

        // Assert
        pilot.NbUserGk.Should().Be(9999);
        pilot.ScreenGk.Should().Be(1111);
    }

    [TestMethod]
    public void ScreenPilot_StatusAndDualMode_ShouldSupportValidValues()
    {
        // Arrange & Act
        var pilot1 = new ScreenPilot { StatusId = 0, DualMode = 0 };
        var pilot2 = new ScreenPilot { StatusId = 1, DualMode = 0 };
        var pilot3 = new ScreenPilot { StatusId = 1, DualMode = 1 };

        // Assert
        pilot1.StatusId.Should().Be(0);
        pilot1.DualMode.Should().Be(0);
        pilot2.StatusId.Should().Be(1);
        pilot2.DualMode.Should().Be(0);
        pilot3.StatusId.Should().Be(1);
        pilot3.DualMode.Should().Be(1);
    }
}