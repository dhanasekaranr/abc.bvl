namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Represents a pilot assignment of a user to a screen.
/// Simplified schema with essential fields only.
/// </summary>
public class ScreenPilot
{
    /// <summary>
    /// Primary key - Screen Pilot Global Key
    /// </summary>
    public long ScreenPilotGk { get; set; }

    /// <summary>
    /// User Global Key (foreign key to user table)
    /// </summary>
    public long NbUserGk { get; set; }

    /// <summary>
    /// Screen Global Key (foreign key to screen definition)
    /// </summary>
    public long ScreenGk { get; set; }

    /// <summary>
    /// Status identifier (1=Active, 0=Inactive)
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    /// Dual mode flag (1=Enabled, 0=Disabled)
    /// </summary>
    public int DualMode { get; set; }

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
    /// Navigation property to the parent screen definition
    /// </summary>
    public virtual ScreenDefinition? ScreenDefinition { get; set; }
}