using abc.bvl.AdminTool.Domain.Entities.Base;

namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// State/Province lookup table - another example of admin lookup table
/// </summary>
public class State : BaseLookupEntity
{
    /// <summary>
    /// State code (e.g., "CA", "NY", "TX") - maps to Code from base
    /// </summary>
    public string StateCode
    {
        get => Code;
        set => Code = value;
    }

    /// <summary>
    /// State name (e.g., "California", "New York") - maps to Name from base
    /// </summary>
    public string StateName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    /// Foreign key to Country
    /// </summary>
    public long CountryId { get; set; }

    /// <summary>
    /// State type (State, Province, Territory, etc.)
    /// </summary>
    public string? StateType { get; set; }

    // Navigation properties
    public virtual Country? Country { get; set; }

    public override string ToString() => $"{StateCode} - {StateName}";
}