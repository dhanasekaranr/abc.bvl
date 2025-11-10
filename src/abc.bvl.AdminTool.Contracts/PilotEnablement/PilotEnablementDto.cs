using MediatR;

namespace abc.bvl.AdminTool.Contracts.PilotEnablement;

/// <summary>
/// Primary DTO for UI - represents a USER (Pilot) with all their screen assignments
/// This is the main data structure for managing user screen access
/// Implements IRequest to work directly with MediatR (no separate Command needed)
/// </summary>
public record PilotEnablementDto : IRequest<PilotEnablementDto>
{
    /// <summary>
    /// User Global Key (Pilot identifier) - NUMBER(9)
    /// </summary>
    public long NbUserGk { get; init; }

    /// <summary>
    /// User display name
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// User email (optional)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// User department or team (optional)
    /// </summary>
    public string? Department { get; init; }

    /// <summary>
    /// List of screens this user has access to
    /// Each item represents a screen assignment with its details
    /// </summary>
    public List<ScreenDefnPilotUserDto> ScreenAssignments { get; init; } = new();

    /// <summary>
    /// Total number of screens assigned to this user
    /// </summary>
    public int TotalScreens => ScreenAssignments?.Count ?? 0;

    /// <summary>
    /// Number of active screen assignments
    /// </summary>
    public int ActiveScreens => ScreenAssignments?.Count(s => s.StatusId == 1) ?? 0;

    /// <summary>
    /// Who requested this operation (for audit) - NUMBER(9)
    /// Populated by the controller, not by UI
    /// </summary>
    public long RequestedBy { get; init; }
}

/// <summary>
/// Represents a single screen assignment for a user
/// Combines ScreenDefinition + ScreenPilot data in one DTO
/// </summary>
public record ScreenDefnPilotUserDto
{
    /// <summary>
    /// ScreenPilot Global Key (assignment ID) - null for new assignments
    /// </summary>
    public long? ScreenPilotGk { get; init; }

    /// <summary>
    /// Screen Global Key (Screen Definition ID)
    /// </summary>
    public long ScreenGk { get; init; }

    /// <summary>
    /// Screen name (from ScreenDefinition)
    /// </summary>
    public string ScreenName { get; init; } = string.Empty;

    /// <summary>
    /// Assignment status: 0=Inactive, 1=Active (from ScreenPilot)
    /// </summary>
    public int StatusId { get; init; }

    /// <summary>
    /// Dual mode flag: 0=Disabled, 1=Enabled (from ScreenPilot)
    /// </summary>
    public int DualMode { get; init; }

    /// <summary>
    /// When this screen was assigned to the user
    /// </summary>
    public DateTime? AssignedDate { get; init; }

    /// <summary>
    /// Who assigned this screen (USER_GK)
    /// </summary>
    public long? AssignedBy { get; init; }

    /// <summary>
    /// Screen definition status (from ScreenDefinition)
    /// </summary>
    public int ScreenStatusId { get; init; }
}
