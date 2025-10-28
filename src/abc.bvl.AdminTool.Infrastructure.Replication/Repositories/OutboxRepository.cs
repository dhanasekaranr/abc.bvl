using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Repositories;

/// <summary>
/// Repository implementation for outbox messages
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AdminDbContext _context;
    private readonly ILogger<OutboxRepository> _logger;

    public OutboxRepository(AdminDbContext context, ILogger<OutboxRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == "Pending")
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OutboxMessage>> GetRetryableMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.OutboxMessages
            .Where(m => m.Status == "Failed" && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(long messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Status = "Completed";
            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Outbox message {MessageId} marked as processed", messageId);
        }
    }

    public async Task MarkAsFailedAsync(long messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Status = "Failed";
            message.Error = error;
            message.RetryCount++;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning("Outbox message {MessageId} marked as failed. RetryCount: {RetryCount}, Error: {Error}", 
                messageId, message.RetryCount, error);
        }
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Outbox message {MessageId} added for {Type} {EntityId}", 
            message.Id, message.Type, message.EntityId);
    }
}
