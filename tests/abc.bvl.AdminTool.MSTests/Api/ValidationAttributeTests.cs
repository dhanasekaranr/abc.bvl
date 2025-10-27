using abc.bvl.AdminTool.Api.Validation;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Api;

[TestClass]
public class SafeStringAttributeTests
{
    private SafeStringAttribute _attribute = null!;

    [TestInitialize]
    public void Setup()
    {
        _attribute = new SafeStringAttribute();
    }

    [TestMethod]
    public void IsValid_WithValidString_ShouldReturnTrue()
    {
        // Arrange
        var validStrings = new[]
        {
            "Hello World",
            "Test123",
            "user@example.com",
            "Product-Name_2024",
            "Price: $99.99",
            "Score: 100%",
            "File (version 1.0)",
            "Data [Array]",
            "Key: Value",
            "Question?",
            "Statement!"
        };

        // Act & Assert
        foreach (var str in validStrings)
        {
            _attribute.IsValid(str).Should().BeTrue($"{str} should be valid");
        }
    }

    [TestMethod]
    public void IsValid_WithNull_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid(null).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithNonString_ShouldReturnFalse()
    {
        // Act & Assert
        _attribute.IsValid(123).Should().BeFalse();
        _attribute.IsValid(new object()).Should().BeFalse();
    }

    [TestMethod]
    public void FormatErrorMessage_ShouldReturnProperMessage()
    {
        // Act
        var message = _attribute.FormatErrorMessage("TestField");

        // Assert
        message.Should().Be("The field TestField contains invalid characters.");
    }
}

[TestClass]
public class ValidEntityIdAttributeTests
{
    private ValidEntityIdAttribute _attribute = null!;

    [TestInitialize]
    public void Setup()
    {
        _attribute = new ValidEntityIdAttribute();
    }

    [TestMethod]
    public void IsValid_WithValidLong_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid(1L).Should().BeTrue();
        _attribute.IsValid(100L).Should().BeTrue();
        _attribute.IsValid(long.MaxValue).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithValidInt_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid(1).Should().BeTrue();
        _attribute.IsValid(100).Should().BeTrue();
        _attribute.IsValid(int.MaxValue).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithValidString_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid("1").Should().BeTrue();
        _attribute.IsValid("12345").Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithZero_ShouldReturnFalse()
    {
        // Act & Assert
        _attribute.IsValid(0L).Should().BeFalse();
        _attribute.IsValid(0).Should().BeFalse();
        _attribute.IsValid("0").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_WithNegative_ShouldReturnFalse()
    {
        // Act & Assert
        _attribute.IsValid(-1L).Should().BeFalse();
        _attribute.IsValid(-100).Should().BeFalse();
        _attribute.IsValid("-1").Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_WithNull_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid(null).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithInvalidString_ShouldReturnFalse()
    {
        // Act & Assert
        _attribute.IsValid("abc").Should().BeFalse();
        _attribute.IsValid("12.5").Should().BeFalse();
    }

    [TestMethod]
    public void FormatErrorMessage_ShouldReturnProperMessage()
    {
        // Act
        var message = _attribute.FormatErrorMessage("Id");

        // Assert
        message.Should().Be("The field Id must be a valid positive integer.");
    }
}

[TestClass]
public class EntityCodeAttributeTests
{
    private EntityCodeAttribute _attribute = null!;

    [TestInitialize]
    public void Setup()
    {
        _attribute = new EntityCodeAttribute();
    }

    [TestMethod]
    public void IsValid_WithValidCode_ShouldReturnTrue()
    {
        // Arrange
        var validCodes = new[]
        {
            "A",
            "ABC",
            "TEST123",
            "CODE-001",
            "USER_ADMIN",
            "A1",
            "Z9"
        };

        // Act & Assert
        foreach (var code in validCodes)
        {
            _attribute.IsValid(code).Should().BeTrue($"{code} should be valid");
        }
    }

    [TestMethod]
    public void IsValid_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var invalidCodes = new[]
        {
            "abc",           // Lowercase
            "Test",          // Mixed case
            "-TEST",         // Starts with hyphen
            "TEST-",         // Ends with hyphen
            "_TEST",         // Starts with underscore
            "TE ST",         // Contains space
            "TEST@CODE"      // Contains invalid character
        };

        // Act & Assert
        foreach (var code in invalidCodes)
        {
            _attribute.IsValid(code).Should().BeFalse($"{code} should be invalid");
        }
    }

    [TestMethod]
    public void IsValid_WithNull_ShouldReturnTrue()
    {
        // Act & Assert
        _attribute.IsValid(null).Should().BeTrue();
    }

    [TestMethod]
    public void IsValid_WithTooLong_ShouldReturnFalse()
    {
        // Arrange
        _attribute.MaxLength = 10;
        var longCode = new string('A', 11);

        // Act & Assert
        _attribute.IsValid(longCode).Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_WithTooShort_ShouldReturnFalse()
    {
        // Arrange
        _attribute.MinLength = 3;

        // Act & Assert
        _attribute.IsValid("AB").Should().BeFalse();
    }

    [TestMethod]
    public void FormatErrorMessage_ShouldReturnProperMessage()
    {
        // Arrange
        _attribute.MinLength = 2;
        _attribute.MaxLength = 10;

        // Act
        var message = _attribute.FormatErrorMessage("Code");

        // Assert
        message.Should().Be("The field Code must be 2-10 characters, uppercase letters, numbers, hyphens and underscores only.");
    }
}
