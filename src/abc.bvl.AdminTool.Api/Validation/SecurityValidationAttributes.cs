using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace abc.bvl.AdminTool.Api.Validation;

/// <summary>
/// Validates that input contains only safe characters (alphanumeric, spaces, and basic punctuation)
/// Prevents XSS and injection attacks
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SafeStringAttribute : ValidationAttribute
{
    private static readonly Regex SafeStringRegex = new(
        @"^[a-zA-Z0-9\s\.\-_@#$%&()\[\]{},:;!?+=/*<>|'""\\~`^]*$", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is not string stringValue) return false;
        
        try
        {
            return SafeStringRegex.IsMatch(stringValue);
        }
        catch (RegexMatchTimeoutException)
        {
            // Regex timeout indicates potential ReDoS attack
            return false;
        }
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} contains invalid characters.";
    }
}

/// <summary>
/// Validates database entity IDs to prevent SQL injection through ID manipulation
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidEntityIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            long longValue => longValue > 0 && longValue <= long.MaxValue,
            int intValue => intValue > 0,
            string stringValue => long.TryParse(stringValue, out var parsed) && parsed > 0,
            _ => false
        };
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} must be a valid positive integer.";
    }
}

/// <summary>
/// Validates entity codes to ensure they follow standard naming conventions
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EntityCodeAttribute : ValidationAttribute
{
    private static readonly Regex CodeRegex = new(
        @"^[A-Z0-9][A-Z0-9_-]*[A-Z0-9]$|^[A-Z0-9]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public int MinLength { get; set; } = 1;
    public int MaxLength { get; set; } = 20;

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is not string stringValue) return false;
        
        if (stringValue.Length < MinLength || stringValue.Length > MaxLength)
            return false;

        try
        {
            return CodeRegex.IsMatch(stringValue);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} must be {MinLength}-{MaxLength} characters, uppercase letters, numbers, hyphens and underscores only.";
    }
}

/// <summary>
/// Validates that string input doesn't contain potential script injection patterns
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NoScriptInjectionAttribute : ValidationAttribute
{
    private static readonly string[] DangerousPatterns = 
    {
        "<script", "</script>", "javascript:", "vbscript:", "onload=", "onerror=", 
        "onclick=", "onmouseover=", "onfocus=", "onblur=", "onsubmit=", "eval(",
        "expression(", "url(", "import(", "setTimeout(", "setInterval(",
        "document.", "window.", "location.", "alert(", "confirm(", "prompt(",
        "innerHTML", "outerHTML", "insertAdjacentHTML", "write(", "writeln("
    };

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is not string stringValue) return false;
        
        var lowerValue = stringValue.ToLowerInvariant();
        return !DangerousPatterns.Any(pattern => lowerValue.Contains(pattern));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} contains potentially dangerous content.";
    }
}

/// <summary>
/// Validates file paths to prevent directory traversal attacks
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SafeFilePathAttribute : ValidationAttribute
{
    private static readonly string[] DangerousPatterns = 
    {
        "..", "~", "/", "\\", ":", "*", "?", "\"", "<", ">", "|"
    };

    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (value is not string stringValue) return false;
        
        return !DangerousPatterns.Any(pattern => stringValue.Contains(pattern));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} contains invalid file path characters.";
    }
}