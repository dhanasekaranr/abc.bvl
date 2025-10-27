using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class BaseAdminEntityTests
{
    // Using ScreenDefinition as concrete implementation of BaseAdminEntity
    
    [TestMethod]
    public void MarkDeleted_ShouldSetStatusToZeroAndUpdateAuditFields()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Id = 1,
            Name = "TestScreen",
            Status = 1,
            CreatedBy = "creator",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        var deletedBy = "deleter";
        var beforeDelete = DateTime.UtcNow;

        // Act
        entity.MarkDeleted(deletedBy);

        // Assert
        entity.Status.Should().Be(0);
        entity.UpdatedBy.Should().Be(deletedBy);
        entity.UpdatedAt.Should().BeOnOrAfter(beforeDelete);
    }

    [TestMethod]
    public void UpdateAuditFields_ForNewEntity_ShouldSetCreatedAndUpdatedFields()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Id = 0, // New entity
            Name = "NewScreen",
            Status = 1
        };
        var user = "test.user";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        entity.UpdateAuditFields(user);

        // Assert
        entity.CreatedBy.Should().Be(user);
        entity.CreatedAt.Should().BeOnOrAfter(beforeUpdate);
        entity.UpdatedBy.Should().Be(user);
        entity.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        entity.CreatedAt.Should().BeCloseTo(entity.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void UpdateAuditFields_ForExistingEntity_ShouldOnlyUpdateUpdatedFields()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-10);
        var originalCreatedBy = "original.creator";
        var entity = new ScreenDefinition
        {
            Id = 123, // Existing entity
            Name = "ExistingScreen",
            Status = 1,
            CreatedBy = originalCreatedBy,
            CreatedAt = originalCreatedAt
        };
        var updater = "updater.user";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        entity.UpdateAuditFields(updater);

        // Assert
        entity.CreatedBy.Should().Be(originalCreatedBy); // Should NOT change
        entity.CreatedAt.Should().Be(originalCreatedAt); // Should NOT change
        entity.UpdatedBy.Should().Be(updater);
        entity.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [TestMethod]
    public void IsActive_WhenStatusIsOne_ShouldReturnTrue()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Name = "ActiveScreen",
            Status = 1,
            CreatedBy = "user"
        };

        // Act & Assert
        entity.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void IsActive_WhenStatusIsZero_ShouldReturnFalse()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Name = "InactiveScreen",
            Status = 0,
            CreatedBy = "user"
        };

        // Act & Assert
        entity.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void IsActive_WhenStatusIsPending_ShouldReturnFalse()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Name = "PendingScreen",
            Status = 2,
            CreatedBy = "user"
        };

        // Act & Assert
        entity.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void Validate_DefaultImplementation_ShouldReturnEmpty()
    {
        // Arrange
        var entity = new ScreenDefinition
        {
            Code = "TEST001", // Required by BaseLookupEntity
            Name = "TestScreen",
            Status = 1,
            CreatedBy = "user"
        };
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(entity);

        // Act
        var results = entity.Validate(validationContext);

        // Assert
        results.Should().BeEmpty();
    }

    [TestMethod]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var entity = new ScreenDefinition
        {
            Name = "Test",
            CreatedBy = "user"
        };

        // Assert
        entity.Status.Should().Be(1); // Default status is Active
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.CreatedBy.Should().NotBeNullOrEmpty();
        entity.UpdatedBy.Should().NotBeNull();
    }

    [TestMethod]
    public void RowVersion_ShouldBeNullableForOptimisticConcurrency()
    {
        // Arrange & Act
        var entity = new ScreenDefinition
        {
            Name = "Test",
            CreatedBy = "user",
            RowVersion = null
        };

        // Assert
        entity.RowVersion.Should().BeNull();
    }
}
