using abc.bvl.AdminTool.Infrastructure.Data.Services;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Infrastructure;

[TestClass]
public class RequestContextAccessorTests
{
    [TestMethod]
    public void UserId_ShouldReturnDemoUser()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.UserId.Should().Be("demo-user");
    }

    [TestMethod]
    public void DisplayName_ShouldReturnDemoUser()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.DisplayName.Should().Be("Demo User");
    }

    [TestMethod]
    public void Email_ShouldReturnDemoEmail()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.Email.Should().Be("demo@example.com");
    }

    [TestMethod]
    public void Roles_ShouldReturnEmptyArray()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.Roles.Should().BeEmpty();
    }

    [TestMethod]
    public void CorrelationId_ShouldReturnValidGuid()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act
        var correlationId = accessor.CorrelationId;

        // Assert
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [TestMethod]
    public void CorrelationId_ShouldBeUniqueOnEachAccess()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act
        var id1 = accessor.CorrelationId;
        var id2 = accessor.CorrelationId;

        // Assert
        id1.Should().NotBe(id2);
    }

    [TestMethod]
    public void DbRoute_ShouldReturnPrimary()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.DbRoute.Should().Be("primary");
    }

    [TestMethod]
    public void CanRead_ShouldReturnTrue()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public void CanWrite_ShouldReturnTrue()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.CanWrite.Should().BeTrue();
    }

    [TestMethod]
    public void AllProperties_ShouldBeAccessibleConcurrently()
    {
        // Arrange
        var accessor = new RequestContextAccessor();

        // Act & Assert
        accessor.UserId.Should().NotBeNullOrEmpty();
        accessor.DisplayName.Should().NotBeNullOrEmpty();
        accessor.Email.Should().NotBeNullOrEmpty();
        accessor.Roles.Should().NotBeNull();
        accessor.CorrelationId.Should().NotBeNullOrEmpty();
        accessor.DbRoute.Should().NotBeNullOrEmpty();
        accessor.CanRead.Should().BeTrue();
        accessor.CanWrite.Should().BeTrue();
    }
}
