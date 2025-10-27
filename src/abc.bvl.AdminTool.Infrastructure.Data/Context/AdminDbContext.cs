using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace abc.bvl.AdminTool.Infrastructure.Data.Context;

/// <summary>
/// Entity Framework DbContext for AdminTool
/// Supports Oracle database with dual-database routing capability
/// </summary>
public class AdminDbContext : DbContext, IAdminDbContext
{
    private IDbContextTransaction? _currentTransaction;

    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<ScreenDefinition> ScreenDefinitions => Set<ScreenDefinition>();
    public DbSet<ScreenPilot> ScreenPilots => Set<ScreenPilot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction?.CommitAsync(cancellationToken)!;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _currentTransaction?.RollbackAsync(cancellationToken)!;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Enqueues a message to the outbox for cross-database synchronization
    /// Implements the Transactional Outbox pattern
    /// </summary>
    public async Task EnqueueOutboxAsync(string type, object payload, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };

        OutboxMessages.Add(outboxMessage);
        await Task.CompletedTask;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations for Oracle database
        modelBuilder.ApplyConfiguration(new ScreenDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ScreenPilotConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}