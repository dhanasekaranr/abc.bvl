using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class StateTests
{
    [TestMethod]
    public void Constructor_ShouldCreateStateWithDefaultValues()
    {
        // Arrange & Act
        var state = new State
        {
            Code = "CA",
            Name = "California",
            CountryId = 1,
            CreatedBy = "admin"
        };

        // Assert
        state.Code.Should().Be("CA");
        state.Name.Should().Be("California");
        state.CountryId.Should().Be(1);
        state.Status.Should().Be(1);
        state.SortOrder.Should().Be(0);
    }

    [TestMethod]
    public void State_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var state = new State
        {
            Id = 5,
            Code = "TX",
            Name = "Texas",
            Description = "Lone Star State",
            CountryId = 1,
            SortOrder = 10,
            Status = 1,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = "admin",
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        state.Id.Should().Be(5);
        state.Code.Should().Be("TX");
        state.Name.Should().Be("Texas");
        state.Description.Should().Be("Lone Star State");
        state.CountryId.Should().Be(1);
        state.SortOrder.Should().Be(10);
        state.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void State_WithCountryRelation_ShouldStoreReference()
    {
        // Arrange
        var country = new Country
        {
            Id = 1,
            Code = "US",
            Name = "United States",
            CreatedBy = "admin"
        };

        var state = new State
        {
            Code = "NY",
            Name = "New York",
            CountryId = country.Id,
            Country = country,
            CreatedBy = "admin"
        };

        // Act & Assert
        state.CountryId.Should().Be(1);
        state.Country.Should().NotBeNull();
        state.Country!.Code.Should().Be("US");
        state.Country.Name.Should().Be("United States");
    }

    [TestMethod]
    public void State_MarkDeleted_ShouldSetInactive()
    {
        // Arrange
        var state = new State
        {
            Code = "FL",
            Name = "Florida",
            CountryId = 1,
            Status = 1,
            CreatedBy = "admin"
        };

        // Act
        state.MarkDeleted("deleter");

        // Assert
        state.Status.Should().Be(0);
        state.UpdatedBy.Should().Be("deleter");
        state.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void State_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var state = new State
        {
            Code = "AZ",
            Name = "Arizona",
            CountryId = 1,
            CreatedBy = "admin"
        };

        // Act
        var result = state.ToString();

        // Assert
        result.Should().Be("AZ - Arizona");
    }

    [TestMethod]
    public void State_Country_ShouldBeNullableForLazyLoading()
    {
        // Arrange & Act
        var state = new State
        {
            Code = "WA",
            Name = "Washington",
            CountryId = 1,
            Country = null,
            CreatedBy = "admin"
        };

        // Assert
        state.Country.Should().BeNull();
        state.CountryId.Should().Be(1); // FK still set
    }
}
