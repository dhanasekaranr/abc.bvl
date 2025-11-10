namespace abc.bvl.AdminTool.Application.Common.Models;

/// <summary>
/// Internal DTO for screen definition data (used by repositories)
/// Not exposed via API - PilotEnablementDto is the public contract
/// </summary>
public record ScreenDefnDto
{
    public long ScreenGk { get; init; }
    public string ScreenName { get; init; } = string.Empty;
    public int StatusId { get; init; }
    public DateTime CreatedDt { get; init; }
    public long CreatedBy { get; init; }
    public DateTime UpdatedDt { get; init; }
    public long UpdatedBy { get; init; }
}
