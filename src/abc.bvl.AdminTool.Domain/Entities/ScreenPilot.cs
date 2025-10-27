using abc.bvl.AdminTool.Domain.Entities.Base;

namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Screen pilot assignment entity - links users to screens they can access
/// </summary>
public class ScreenPilot : BaseAdminEntity
{
    /// <summary>
    /// Foreign key to ScreenDefinition
    /// </summary>
    public long ScreenDefnId { get; set; }

    /// <summary>
    /// User identifier who has access to the screen
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Access level or role for this user-screen combination
    /// </summary>
    public string? AccessLevel { get; set; }

    // Navigation properties
    public virtual ScreenDefinition ScreenDefinition { get; set; } = null!;
}