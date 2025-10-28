using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;
using Microsoft.Extensions.Logging;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Services;

/// <summary>
/// Service for publishing events to the outbox
/// Messages are saved in the same transaction as domain changes
/// </summary>
public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _repository;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IOutboxRepository repository, ILogger<OutboxPublisher> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        await _repository.AddAsync(message, cancellationToken);
        
        _logger.LogInformation(
            "Published outbox message: Type={Type}, EntityId={EntityId}, Operation={Operation}",
            message.Type, message.EntityId, message.Operation);
    }

    public async Task PublishBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            return;
        }

        foreach (var message in messages)
        {
            await _repository.AddAsync(message, cancellationToken);
        }

        _logger.LogInformation("Published {Count} outbox messages", messages.Count());
    }
}
