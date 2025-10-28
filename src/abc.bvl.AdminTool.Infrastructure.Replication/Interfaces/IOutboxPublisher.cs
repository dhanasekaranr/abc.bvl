using abc.bvl.AdminTool.Domain.Entities;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;

/// <summary>
/// Service for publishing domain events to the outbox
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publish an event to the outbox for eventual replication
    /// </summary>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish multiple events in a batch
    /// </summary>
    Task PublishBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);
}
