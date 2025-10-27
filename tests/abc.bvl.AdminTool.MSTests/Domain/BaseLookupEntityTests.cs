using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class BaseLookupEntityTests
{
    // Using Country as concrete implementation of BaseLookupEntity
    
    [TestMethod]
    public void Constructor_ShouldCreateLookupEntityWithCodeAndName()
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
    }

    [TestMethod]
    public void Validate_WithValidCodeAndName_ShouldReturnNoErrors()
    {
        // Arrange
        var country = new Country
        {
            Code = "CA",
            Name = "Canada",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext);

        // Assert
        results.Should().BeEmpty();
    }

    [TestMethod]
    public void Validate_WithEmptyCode_ShouldReturnValidationError()
    {
        // Arrange
        var country = new Country
        {
            Code = "",
            Name = "Test Country",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].ErrorMessage.Should().Be("Code is required");
        results[0].MemberNames.Should().Contain("Code");
    }

    [TestMethod]
    public void Validate_WithWhitespaceCode_ShouldReturnValidationError()
    {
        // Arrange
        var country = new Country
        {
            Code = "   ",
            Name = "Test Country",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].ErrorMessage.Should().Be("Code is required");
    }

    [TestMethod]
    public void Validate_WithEmptyName_ShouldReturnValidationError()
    {
        // Arrange
        var country = new Country
        {
            Code = "UK",
            Name = "",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].ErrorMessage.Should().Be("Name is required");
        results[0].MemberNames.Should().Contain("Name");
    }

    [TestMethod]
    public void Validate_WithWhitespaceName_ShouldReturnValidationError()
    {
        // Arrange
        var country = new Country
        {
            Code = "FR",
            Name = "   ",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].ErrorMessage.Should().Be("Name is required");
    }

    [TestMethod]
    public void Validate_WithBothCodeAndNameEmpty_ShouldReturnTwoValidationErrors()
    {
        // Arrange
        var country = new Country
        {
            Code = "",
            Name = "",
            CreatedBy = "admin"
        };
        var validationContext = new ValidationContext(country);

        // Act
        var results = country.Validate(validationContext).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.ErrorMessage == "Code is required");
        results.Should().Contain(r => r.ErrorMessage == "Name is required");
    }

    [TestMethod]
    public void ToString_ShouldReturnFormattedCodeAndName()
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

    [TestMethod]
    public void Description_ShouldBeOptional()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "DE",
            Name = "Germany",
            Description = null,
            CreatedBy = "admin"
        };

        // Assert
        country.Description.Should().BeNull();
    }

    [TestMethod]
    public void Description_WhenProvided_ShouldBeStored()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "IN",
            Name = "India",
            Description = "Republic of India",
            CreatedBy = "admin"
        };

        // Assert
        country.Description.Should().Be("Republic of India");
    }

    [TestMethod]
    public void SortOrder_ShouldDefaultToZero()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "BR",
            Name = "Brazil",
            CreatedBy = "admin"
        };

        // Assert
        country.SortOrder.Should().Be(0);
    }

    [TestMethod]
    public void SortOrder_ShouldBeSettable()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "AU",
            Name = "Australia",
            SortOrder = 5,
            CreatedBy = "admin"
        };

        // Assert
        country.SortOrder.Should().Be(5);
    }

    [TestMethod]
    public void InheritsFromBaseAdminEntity_ShouldHaveAuditFields()
    {
        // Arrange & Act
        var country = new Country
        {
            Code = "CN",
            Name = "China",
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = "updater",
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        country.CreatedBy.Should().NotBeNullOrEmpty();
        country.UpdatedBy.Should().NotBeNullOrEmpty();
        country.Status.Should().Be(1);
    }
}
