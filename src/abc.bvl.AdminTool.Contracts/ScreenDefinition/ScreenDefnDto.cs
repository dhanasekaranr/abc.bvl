namespace abc.bvl.AdminTool.Contracts.ScreenDefinition;

/// <summary>
/// Screen Definition Data Transfer Object
/// Lightweight DTO for transferring screen definition data
/// Note: Does NOT inherit from BasePageDto - use PagedResult<ScreenDefnDto> or SingleResult<ScreenDefnDto> for responses
/// </summary>
public record ScreenDefnDto(
    long? Id,                    // Nullable for create operations
    string Name,
    byte Status,
    DateTimeOffset? CreatedAt = null,   // Nullable for create operations
    string? CreatedBy = null,           // Nullable for create operations  
    DateTimeOffset? UpdatedAt = null,   // Nullable for create operations
    string? UpdatedBy = null            // Nullable for create operations
);