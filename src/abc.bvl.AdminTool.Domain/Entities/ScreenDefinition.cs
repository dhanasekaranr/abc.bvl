namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Represents a screen definition in the system.
/// Simplified schema with essential fields only.
/// </summary>
public class ScreenDefinition
{
    /// <summary>
    /// Primary key - Screen Global Key
    /// </summary>
    public long ScreenGk { get; set; }

    /// <summary>
    /// Screen name (max 50 characters)
    /// </summary>
    public string ScreenName { get; set; } = string.Empty;

    /// <summary>
    /// Status identifier (1=Active, 0=Inactive)
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    /// Created date/time
    /// </summary>
    public DateTime CreatedDt { get; set; }

    /// <summary>
    /// User ID who created the record
    /// </summary>
    public long CreatedBy { get; set; }

    /// <summary>
    /// Last updated date/time
    /// </summary>
    public DateTime UpdatedDt { get; set; }

    /// <summary>
    /// User ID who last updated the record
    /// </summary>
    public long UpdatedBy { get; set; }

    /// <summary>
    /// Collection of pilot assignments for this screen
    /// </summary>
    public virtual ICollection<ScreenPilot> ScreenPilots { get; set; } = new List<ScreenPilot>();
}