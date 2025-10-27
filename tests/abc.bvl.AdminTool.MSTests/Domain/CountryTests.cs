using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class CountryTests
{
    [TestMethod]
    public void Constructor_ShouldCreateCountryWithDefaultValues()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "US",
            Name = "United States",
            CreatedBy = "admin"
        };

        // Assert
        country.Code.Should().Be("US");
        country.Name.Should().Be("United States");
        country.Status.Should().Be(1);
        country.SortOrder.Should().Be(0);
        country.Description.Should().BeNull();
    }

    [TestMethod]
    public void Country_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var country = new Country
        {
            Id = 1,
            Code = "CA",
            Name = "Canada",
            Description = "North American country",
            SortOrder = 2,
            Status = 1,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = "admin",
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        country.Id.Should().Be(1);
        country.Code.Should().Be("CA");
        country.Name.Should().Be("Canada");
        country.Description.Should().Be("North American country");
        country.SortOrder.Should().Be(2);
        country.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void Country_MarkDeleted_ShouldSetInactive()
    {
        // Arrange
        var country = new Country
        {
            Code = "UK",
            Name = "United Kingdom",
            Status = 1,
            CreatedBy = "admin"
        };

        // Act
        country.MarkDeleted("deleter");

        // Assert
        country.Status.Should().Be(0);
        country.UpdatedBy.Should().Be("deleter");
        country.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void Country_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var country = new Country
        {
            Code = "JP",
            Name = "Japan",
            CreatedBy = "admin"
        };

        // Act
        var result = country.ToString();

        // Assert
        result.Should().Be("JP - Japan");
    }
}
