using FluentValidation;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace abc.bvl.AdminTool.Api.Validation;

/// <summary>
/// Comprehensive validator for ScreenDefnDto with security validation
/// </summary>
public class ScreenDefnDtoValidator : AbstractValidator<ScreenDefnDto>
{
    private static readonly Regex SafeStringRegex = new(
        @"^[a-zA-Z0-9\s\.\-_@#$%&()\[\]{},:;!?+=/*<>|'""\\~`^]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CodeRegex = new(
        @"^[A-Z0-9][A-Z0-9_-]*[A-Z0-9]$|^[A-Z0-9]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ScreenDefnDtoValidator()
    {
        // ID validation
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .When(x => x.Id.HasValue)
            .WithMessage("ID must be a positive number when provided");

        // Name validation (primary business field)
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Screen name is required")
            .Length(1, 100)
            .WithMessage("Screen name must be between 1 and 100 characters")
            .Must(BeSafeString)
            .WithMessage("Screen name contains invalid characters")
            .Must(NotContainScriptTags)
            .WithMessage("Screen name cannot contain script content");

        // Status validation
        RuleFor(x => x.Status)
            .InclusiveBetween((byte)0, (byte)2)
            .WithMessage("Status must be 0 (Inactive), 1 (Active), or 2 (Pending)");

        // Audit field validation
        RuleFor(x => x.CreatedBy)
            .Length(1, 50)
            .WithMessage("Created by must be between 1 and 50 characters")
            .Must(BeSafeString!)
            .When(x => !string.IsNullOrEmpty(x.CreatedBy))
            .WithMessage("Created by contains invalid characters");

        RuleFor(x => x.UpdatedBy)
            .Length(1, 50)
            .WithMessage("Updated by must be between 1 and 50 characters")  
            .Must(BeSafeString!)
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy))
            .WithMessage("Updated by contains invalid characters");

        // Date validation
        RuleFor(x => x.CreatedAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .When(x => x.CreatedAt.HasValue)
            .WithMessage("Created date cannot be in the future");

        RuleFor(x => x.UpdatedAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .When(x => x.UpdatedAt.HasValue)
            .WithMessage("Updated date cannot be in the future");

        // Business rule: UpdatedAt should be >= CreatedAt
        RuleFor(x => x.UpdatedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .When(x => x.CreatedAt.HasValue && x.UpdatedAt.HasValue)
            .WithMessage("Updated date must be after or equal to created date");
    }

    private static bool BeValidCode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        
        try
        {
            return CodeRegex.IsMatch(code);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool BeSafeString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        
        try
        {
            return SafeStringRegex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool NotContainScriptTags(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        
        var lowerValue = value.ToLowerInvariant();
        return !lowerValue.Contains("<script") && 
               !lowerValue.Contains("javascript:") &&
               !lowerValue.Contains("vbscript:") &&
               !lowerValue.Contains("onload=") &&
               !lowerValue.Contains("onerror=");
    }

    private static bool NotContainConsecutiveSpecialChars(string? code)
    {
        if (string.IsNullOrEmpty(code)) return true;
        
        return !code.Contains("--") && 
               !code.Contains("__") && 
               !code.Contains("-_") && 
               !code.Contains("_-");
    }
}

/// <summary>
/// Base validator for common entity properties
/// </summary>
public abstract class BaseEntityValidator<T> : AbstractValidator<T>
{
    protected void ConfigureCommonRules<TProperty>(Expression<Func<T, TProperty>> idExpression) 
        where TProperty : struct
    {
        RuleFor(idExpression)
            .NotEmpty()
            .WithMessage("ID is required");
    }

    protected void ConfigureAuditRules(
        Expression<Func<T, string?>> createdByExpression,
        Expression<Func<T, string?>> updatedByExpression)
    {
        RuleFor(createdByExpression)
            .Length(1, 50)
            .When(x => !string.IsNullOrEmpty(createdByExpression.Compile()(x)))
            .WithMessage("Created by must be between 1 and 50 characters");

        RuleFor(updatedByExpression)
            .Length(1, 50)
            .When(x => !string.IsNullOrEmpty(updatedByExpression.Compile()(x)))
            .WithMessage("Updated by must be between 1 and 50 characters");
    }
}