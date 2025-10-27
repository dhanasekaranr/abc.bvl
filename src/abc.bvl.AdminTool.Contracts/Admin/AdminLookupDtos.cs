namespace abc.bvl.AdminTool.Contracts.Admin;

/// <summary>
/// Country DTO - single DTO handles all CRUD operations (create, read, update)
/// Nullable properties allow flexibility for different operations
/// </summary>
public record CountryDto(
    long? Id = null,                    // Null for create, populated for read/update
    string? CountryCode = null,         // Required for create/update
    string? CountryName = null,         // Required for create/update
    string? Iso3Code = null,           // Optional
    int? NumericCode = null,           // Optional
    string? Region = null,             // Optional
    string? PhoneCode = null,          // Optional
    string? Description = null,        // Optional description
    int SortOrder = 0,                 // Display order
    byte? Status = null,               // 0=Inactive, 1=Active (default)
    DateTimeOffset? CreatedAt = null,   // Audit fields - populated by system
    string? CreatedBy = null,
    DateTimeOffset? UpdatedAt = null,
    string? UpdatedBy = null
);

/// <summary>
/// State DTO - handles all state/province CRUD operations
/// </summary>
public record StateDto(
    long? Id = null,
    string? StateCode = null,          // Required for create/update
    string? StateName = null,          // Required for create/update
    long? CountryId = null,            // Required - FK to Country
    string? StateType = null,          // Optional (State, Province, Territory)
    string? Description = null,        // Optional description
    int SortOrder = 0,                 // Display order
    byte? Status = null,               // 0=Inactive, 1=Active (default)
    DateTimeOffset? CreatedAt = null,   // Audit fields
    string? CreatedBy = null,
    DateTimeOffset? UpdatedAt = null,
    string? UpdatedBy = null,
    
    // Navigation data (read-only)
    string? CountryCode = null,        // From Country.Code
    string? CountryName = null         // From Country.Name
);

/// <summary>
/// Generic lookup DTO - can be used for simple Code/Name lookup tables
/// </summary>
public record LookupDto(
    long? Id = null,
    string? Code = null,               // Required for create/update
    string? Name = null,               // Required for create/update
    string? Description = null,        // Optional
    int SortOrder = 0,                 // Display order
    byte? Status = null,               // 0=Inactive, 1=Active (default)
    DateTimeOffset? CreatedAt = null,   // Audit fields
    string? CreatedBy = null,
    DateTimeOffset? UpdatedAt = null,
    string? UpdatedBy = null
);

/// <summary>
/// Base admin DTO for entities that inherit from BaseAdminEntity
/// </summary>
public record BaseAdminDto(
    long? Id = null,
    byte? Status = null,               // 0=Inactive, 1=Active (default)
    DateTimeOffset? CreatedAt = null,   // Audit fields
    string? CreatedBy = null,
    DateTimeOffset? UpdatedAt = null,
    string? UpdatedBy = null
);