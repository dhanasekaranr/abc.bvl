using System.ComponentModel.DataAnnotations;

namespace abc.bvl.AdminTool.Domain.Entities.Base;

/// <summary>
/// Base entity for all admin lookup tables providing common audit and status properties
/// This enables generic CRUD operations across 100+ tables while maintaining consistency
/// </summary>
public abstract class BaseAdminEntity
{
    /// <summary>
    /// Primary key for all admin entities
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Status of the record (0=Inactive, 1=Active, 2=Pending, etc.)
    /// Enables soft delete and workflow management
    /// </summary>
    [Required]
    public byte Status { get; set; } = 1;

    /// <summary>
    /// Audit trail: When the record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Audit trail: Who created the record
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Audit trail: When the record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Audit trail: Who last updated the record
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Virtual method for custom validation logic in derived entities
    /// </summary>
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return Enumerable.Empty<ValidationResult>();
    }

    /// <summary>
    /// Mark entity as deleted (soft delete)
    /// </summary>
    public virtual void MarkDeleted(string deletedBy)
    {
        Status = 0; // Inactive
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }

    /// <summary>
    /// Check if entity is active
    /// </summary>
    public bool IsActive => Status == 1;

    /// <summary>
    /// Update audit fields before saving
    /// </summary>
    public virtual void UpdateAuditFields(string updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        
        // Set CreatedBy if this is a new entity
        if (Id == 0)
        {
            CreatedAt = DateTime.UtcNow;
            CreatedBy = updatedBy;
        }
    }
}

/// <summary>
/// Base entity for lookup tables with code and name pattern
/// Most admin lookup tables follow Code + Name pattern (Country, State, Category, etc.)
/// </summary>
public abstract class BaseLookupEntity : BaseAdminEntity
{
    /// <summary>
    /// Unique code for the lookup item (e.g., "US", "CA", "UK")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the lookup item (e.g., "United States", "Canada", "United Kingdom")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for additional context
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting in lists
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Override validation to ensure Code uniqueness
    /// </summary>
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = base.Validate(validationContext).ToList();

        if (string.IsNullOrWhiteSpace(Code))
        {
            results.Add(new ValidationResult("Code is required", new[] { nameof(Code) }));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            results.Add(new ValidationResult("Name is required", new[] { nameof(Name) }));
        }

        return results;
    }

    public override string ToString() => $"{Code} - {Name}";
}