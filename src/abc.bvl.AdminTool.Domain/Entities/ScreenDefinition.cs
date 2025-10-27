using abc.bvl.AdminTool.Domain.Entities.Base;

namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Screen definition entity - inherits from BaseLookupEntity for consistent admin table pattern
/// </summary>
public class ScreenDefinition : BaseLookupEntity
{
    /// <summary>
    /// Screen name (maps to Name property from base)
    /// </summary>
    public string ScreenName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    /// Screen code (maps to Code property from base)
    /// </summary>
    public string ScreenCode
    {
        get => Code;
        set => Code = value;
    }

    // Navigation properties
    public virtual ICollection<ScreenPilot> ScreenPilots { get; set; } = new List<ScreenPilot>();
}