namespace abc.bvl.AdminTool.Infrastructure.Replication.Configuration;

/// <summary>
/// Configuration settings for the Outbox pattern processor
/// </summary>
public class OutboxSettings
{
    /// <summary>
    /// Whether the outbox processor is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in seconds for checking outbox messages
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of messages to process in a single batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of retry attempts for failed messages
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Delay in minutes before retrying failed messages
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Connection string for the secondary database
    /// </summary>
    public string SecondaryConnectionString { get; set; } = string.Empty;
}
