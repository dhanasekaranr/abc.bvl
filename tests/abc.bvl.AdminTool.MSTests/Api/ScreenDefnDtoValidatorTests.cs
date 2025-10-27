using abc.bvl.AdminTool.Api.Validation;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace abc.bvl.AdminTool.MSTests.Api;

[TestClass]
public class ScreenDefnDtoValidatorTests
{
    private ScreenDefnDtoValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new ScreenDefnDtoValidator();
    }

    [TestMethod]
    public void Validate_WithValidDto_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            "ValidScreenName",
            1,
            DateTimeOffset.UtcNow.AddDays(-1), // Past date
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            "",
            1,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Validate_WithNullName_ShouldHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            null!,
            1,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Validate_WithNameTooLong_ShouldHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            new string('A', 101),
            1,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Validate_WithInvalidStatus_ShouldHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            "ValidName",
            99,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [TestMethod]
    public void Validate_WithScriptTagInName_ShouldHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            "<script>alert('xss')</script>",
            1,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Validate_WithValidStatuses_ShouldNotHaveErrors()
    {
        // Arrange & Act & Assert
        foreach (byte status in new byte[] { 0, 1, 2 })
        {
            var dto = new ScreenDefnDto(
                1,
                "ValidName",
                status,
                DateTimeOffset.UtcNow,
                "TestUser",
                null,
                null
            );

            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }
    }

    [TestMethod]
    public void Validate_WithMaxLengthName_ShouldNotHaveError()
    {
        // Arrange
        var dto = new ScreenDefnDto(
            1,
            new string('A', 100),
            1,
            DateTimeOffset.UtcNow,
            "TestUser",
            null,
            null
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
