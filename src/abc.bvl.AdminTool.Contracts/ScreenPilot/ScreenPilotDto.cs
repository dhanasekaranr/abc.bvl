namespace abc.bvl.AdminTool.Contracts.ScreenPilot;

public record ScreenPilotDto(
    long? Id,                    // Nullable for create operations
    long ScreenDefnId,
    string UserId,
    byte Status,
    DateTimeOffset? UpdatedAt,   // Nullable for create operations
    string? UpdatedBy,           // Nullable for create operations
    string? RowVersion,          // Nullable for create operations
    string? ScreenName = null    // Optional for joined views
);