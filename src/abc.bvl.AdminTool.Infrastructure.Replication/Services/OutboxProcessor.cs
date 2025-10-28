using abc.bvl.AdminTool.Infrastructure.Replication.Configuration;
using abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Services;

/// <summary>
/// Background service that processes outbox messages and replicates to secondary database
/// Runs on a polling interval to ensure eventual consistency
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxSettings _settings;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxSettings> settings)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Outbox Processor is disabled");
            return;
        }

        _logger.LogInformation("Outbox Processor started. Polling interval: {Interval} seconds, Batch size: {BatchSize}", 
            _settings.PollingIntervalSeconds, _settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        // Get pending messages
        var pendingMessages = await repository.GetPendingMessagesAsync(_settings.BatchSize, cancellationToken);
        
        if (!pendingMessages.Any())
        {
            _logger.LogDebug("No pending outbox messages");
            return;
        }

        _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count());

        foreach (var message in pendingMessages)
        {
            try
            {
                // Replicate to secondary database
                await ReplicateToSecondaryDatabaseAsync(message, cancellationToken);

                // Mark as processed
                await repository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId}: {Type} {EntityId} - {Operation}",
                    message.Id, message.Type, message.EntityId, message.Operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process outbox message {MessageId}: {Type} {EntityId}",
                    message.Id, message.Type, message.EntityId);

                await repository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ReplicateToSecondaryDatabaseAsync(Domain.Entities.OutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var replicator = scope.ServiceProvider.GetRequiredService<IEntityReplicator>();

        _logger.LogInformation(
            "Replicating to secondary DB: {Type} {EntityId} - {Operation}",
            message.Type, message.EntityId, message.Operation);

        try
        {
            await replicator.ReplicateAsync(
                message.Type ?? string.Empty,
                message.Operation ?? string.Empty,
                message.Payload ?? string.Empty,
                cancellationToken);

            _logger.LogInformation(
                "Successfully replicated {Type} {EntityId} - {Operation}",
                message.Type, message.EntityId, message.Operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to replicate {Type} {EntityId} - {Operation}",
                message.Type, message.EntityId, message.Operation);
            throw;
        }
    }
}
