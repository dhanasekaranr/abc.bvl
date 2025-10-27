using abc.bvl.AdminTool.Domain.Entities.Base;

namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Country lookup table - example of how all admin lookup tables should be structured
/// Inherits from BaseLookupEntity for consistent Code/Name pattern
/// </summary>
public class Country : BaseLookupEntity
{
    /// <summary>
    /// ISO country code (e.g., "US", "CA", "UK") - maps to Code from base
    /// </summary>
    public string CountryCode
    {
        get => Code;
        set => Code = value;
    }

    /// <summary>
    /// Country name (e.g., "United States", "Canada") - maps to Name from base
    /// </summary>
    public string CountryName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    /// ISO 3-letter country code (optional)
    /// </summary>
    public string? Iso3Code { get; set; }

    /// <summary>
    /// Numeric country code (optional)
    /// </summary>
    public int? NumericCode { get; set; }

    /// <summary>
    /// Region/Continent (e.g., "North America", "Europe")
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Phone country code (e.g., "+1", "+44")
    /// </summary>
    public string? PhoneCode { get; set; }

    // Navigation properties for related entities
    public virtual ICollection<State> States { get; set; } = new List<State>();

    public override string ToString() => $"{CountryCode} - {CountryName}";
}