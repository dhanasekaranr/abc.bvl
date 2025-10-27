namespace abc.bvl.AdminTool.Domain.Entities;

/// <summary>
/// Outbox message for transactional dual-database synchronization
/// </summary>
public class OutboxMessage
{
    public long Id { get; set; }
    
    /// <summary>
    /// Entity type (e.g., "ScreenDefinition", "ScreenPilot")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity being synchronized
    /// </summary>
    public long EntityId { get; set; }
    
    /// <summary>
    /// Operation type (INSERT, UPDATE, DELETE)
    /// </summary>
    public string Operation { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON payload of the entity
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the message was successfully processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Processing status (Pending, Processing, Completed, Failed)
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Source database (e.g., "primarydb")
    /// </summary>
    public string? SourceDatabase { get; set; }
    
    /// <summary>
    /// Target database (e.g., "secondarydb")
    /// </summary>
    public string? TargetDatabase { get; set; }
    
    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    public string? CorrelationId { get; set; }
}
