namespace abc.bvl.AdminTool.Application.Common.Models;

/// <summary>
/// Internal DTO for screen pilot data (used by repositories)
/// Not exposed via API - PilotEnablementDto is the public contract
/// </summary>
public record ScreenPilotDto
{
    public long ScreenPilotGk { get; init; }
    public long NbUserGk { get; init; }
    public long ScreenGk { get; init; }
    public int StatusId { get; init; }
    public int DualMode { get; init; }
    public DateTime CreatedDt { get; init; }
    public long CreatedBy { get; init; }
    public DateTime UpdatedDt { get; init; }
    public long UpdatedBy { get; init; }
}
