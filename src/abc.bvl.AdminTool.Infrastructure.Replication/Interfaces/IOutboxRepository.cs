using abc.bvl.AdminTool.Domain.Entities;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;

/// <summary>
/// Repository for outbox message operations
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Get pending outbox messages (not yet processed)
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed messages eligible for retry
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetRetryableMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark message as processed
    /// </summary>
    Task MarkAsProcessedAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark message as failed
    /// </summary>
    Task MarkAsFailedAsync(long messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new outbox message
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
