using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Replication.Context;
using abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Services;

/// <summary>
/// Service for replicating entities to secondary database
/// </summary>
public class EntityReplicator : IEntityReplicator
{
    private readonly SecondaryDbContext _context;
    private readonly ILogger<EntityReplicator> _logger;

    public EntityReplicator(
        SecondaryDbContext context,
        ILogger<EntityReplicator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ReplicateAsync(
        string entityType,
        string operation,
        string payload,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Replicating {EntityType} - {Operation}", entityType, operation);

        switch (entityType)
        {
            case "ScreenDefinition":
                await ReplicateScreenDefinitionAsync(operation, payload, cancellationToken);
                break;

            case "ScreenPilot":
                await ReplicateScreenPilotAsync(operation, payload, cancellationToken);
                break;

            case "Country":
                await ReplicateCountryAsync(operation, payload, cancellationToken);
                break;

            case "State":
                await ReplicateStateAsync(operation, payload, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown entity type for replication: {EntityType}", entityType);
                throw new InvalidOperationException($"Unknown entity type: {entityType}");
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully replicated {EntityType} - {Operation}", entityType, operation);
    }

    private async Task ReplicateScreenDefinitionAsync(
        string operation,
        string payload,
        CancellationToken cancellationToken)
    {
        var entity = JsonSerializer.Deserialize<ScreenDefinition>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize ScreenDefinition");

        switch (operation)
        {
            case "INSERT":
                // Check if entity already exists (idempotency)
                var existing = await _context.ScreenDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ScreenGk == entity.ScreenGk, cancellationToken);

                if (existing == null)
                {
                    _context.ScreenDefinitions.Add(entity);
                    _logger.LogDebug("INSERT ScreenDefinition {ScreenGk}", entity.ScreenGk);
                }
                else
                {
                    _logger.LogDebug("ScreenDefinition {ScreenGk} already exists, skipping INSERT", entity.ScreenGk);
                }
                break;

            case "UPDATE":
                var existingEntity = await _context.ScreenDefinitions
                    .FirstOrDefaultAsync(e => e.ScreenGk == entity.ScreenGk, cancellationToken);

                if (existingEntity != null)
                {
                    // Update properties
                    existingEntity.ScreenName = entity.ScreenName;
                    existingEntity.StatusId = entity.StatusId;
                    existingEntity.UpdatedDt = entity.UpdatedDt;
                    existingEntity.UpdatedBy = entity.UpdatedBy;

                    _context.ScreenDefinitions.Update(existingEntity);
                    _logger.LogDebug("UPDATE ScreenDefinition {ScreenGk}", entity.ScreenGk);
                }
                else
                {
                    // If entity doesn't exist, insert it (recovery scenario)
                    _context.ScreenDefinitions.Add(entity);
                    _logger.LogWarning("ScreenDefinition {ScreenGk} not found for UPDATE, inserting instead", entity.ScreenGk);
                }
                break;

            case "DELETE":
                var entityToDelete = await _context.ScreenDefinitions
                    .FirstOrDefaultAsync(e => e.ScreenGk == entity.ScreenGk, cancellationToken);

                if (entityToDelete != null)
                {
                    // Soft delete by setting status to 0
                    entityToDelete.StatusId = 0;
                    entityToDelete.UpdatedDt = entity.UpdatedDt;
                    entityToDelete.UpdatedBy = entity.UpdatedBy;

                    _context.ScreenDefinitions.Update(entityToDelete);
                    _logger.LogDebug("DELETE (soft) ScreenDefinition {ScreenGk}", entity.ScreenGk);
                }
                else
                {
                    _logger.LogDebug("ScreenDefinition {ScreenGk} not found for DELETE, skipping", entity.ScreenGk);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown operation: {operation}");
        }
    }

    private async Task ReplicateScreenPilotAsync(
        string operation,
        string payload,
        CancellationToken cancellationToken)
    {
        var entity = JsonSerializer.Deserialize<ScreenPilot>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize ScreenPilot");

        switch (operation)
        {
            case "INSERT":
                var existing = await _context.ScreenPilots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ScreenPilotGk == entity.ScreenPilotGk, cancellationToken);

                if (existing == null)
                {
                    _context.ScreenPilots.Add(entity);
                    _logger.LogDebug("INSERT ScreenPilot {ScreenPilotGk}", entity.ScreenPilotGk);
                }
                else
                {
                    _logger.LogDebug("ScreenPilot {ScreenPilotGk} already exists, skipping INSERT", entity.ScreenPilotGk);
                }
                break;

            case "UPDATE":
                var existingEntity = await _context.ScreenPilots
                    .FirstOrDefaultAsync(e => e.ScreenPilotGk == entity.ScreenPilotGk, cancellationToken);

                if (existingEntity != null)
                {
                    existingEntity.NbUserGk = entity.NbUserGk;
                    existingEntity.ScreenGk = entity.ScreenGk;
                    existingEntity.StatusId = entity.StatusId;
                    existingEntity.DualMode = entity.DualMode;
                    existingEntity.UpdatedDt = entity.UpdatedDt;
                    existingEntity.UpdatedBy = entity.UpdatedBy;

                    _context.ScreenPilots.Update(existingEntity);
                    _logger.LogDebug("UPDATE ScreenPilot {ScreenPilotGk}", entity.ScreenPilotGk);
                }
                else
                {
                    _context.ScreenPilots.Add(entity);
                    _logger.LogWarning("ScreenPilot {ScreenPilotGk} not found for UPDATE, inserting instead", entity.ScreenPilotGk);
                }
                break;

            case "DELETE":
                var entityToDelete = await _context.ScreenPilots
                    .FirstOrDefaultAsync(e => e.ScreenPilotGk == entity.ScreenPilotGk, cancellationToken);

                if (entityToDelete != null)
                {
                    entityToDelete.StatusId = 0;
                    entityToDelete.UpdatedDt = entity.UpdatedDt;
                    entityToDelete.UpdatedBy = entity.UpdatedBy;

                    _context.ScreenPilots.Update(entityToDelete);
                    _logger.LogDebug("DELETE (soft) ScreenPilot {ScreenPilotGk}", entity.ScreenPilotGk);
                }
                else
                {
                    _logger.LogDebug("ScreenPilot {ScreenPilotGk} not found for DELETE, skipping", entity.ScreenPilotGk);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown operation: {operation}");
        }
    }

    private async Task ReplicateCountryAsync(
        string operation,
        string payload,
        CancellationToken cancellationToken)
    {
        var entity = JsonSerializer.Deserialize<Country>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize Country");

        switch (operation)
        {
            case "INSERT":
                var existing = await _context.Countries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (existing == null)
                {
                    _context.Countries.Add(entity);
                    _logger.LogDebug("INSERT Country {Id}", entity.Id);
                }
                else
                {
                    _logger.LogDebug("Country {Id} already exists, skipping INSERT", entity.Id);
                }
                break;

            case "UPDATE":
                var existingEntity = await _context.Countries
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (existingEntity != null)
                {
                    existingEntity.Code = entity.Code;
                    existingEntity.Name = entity.Name;
                    existingEntity.Status = entity.Status;
                    existingEntity.UpdatedAt = entity.UpdatedAt;
                    existingEntity.UpdatedBy = entity.UpdatedBy;

                    _context.Countries.Update(existingEntity);
                    _logger.LogDebug("UPDATE Country {Id}", entity.Id);
                }
                else
                {
                    _context.Countries.Add(entity);
                    _logger.LogWarning("Country {Id} not found for UPDATE, inserting instead", entity.Id);
                }
                break;

            case "DELETE":
                var entityToDelete = await _context.Countries
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (entityToDelete != null)
                {
                    entityToDelete.Status = 0;
                    entityToDelete.UpdatedAt = entity.UpdatedAt;
                    entityToDelete.UpdatedBy = entity.UpdatedBy;

                    _context.Countries.Update(entityToDelete);
                    _logger.LogDebug("DELETE (soft) Country {Id}", entity.Id);
                }
                else
                {
                    _logger.LogDebug("Country {Id} not found for DELETE, skipping", entity.Id);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown operation: {operation}");
        }
    }

    private async Task ReplicateStateAsync(
        string operation,
        string payload,
        CancellationToken cancellationToken)
    {
        var entity = JsonSerializer.Deserialize<State>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize State");

        switch (operation)
        {
            case "INSERT":
                var existing = await _context.States
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (existing == null)
                {
                    _context.States.Add(entity);
                    _logger.LogDebug("INSERT State {Id}", entity.Id);
                }
                else
                {
                    _logger.LogDebug("State {Id} already exists, skipping INSERT", entity.Id);
                }
                break;

            case "UPDATE":
                var existingEntity = await _context.States
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (existingEntity != null)
                {
                    existingEntity.Code = entity.Code;
                    existingEntity.Name = entity.Name;
                    existingEntity.CountryId = entity.CountryId;
                    existingEntity.Status = entity.Status;
                    existingEntity.UpdatedAt = entity.UpdatedAt;
                    existingEntity.UpdatedBy = entity.UpdatedBy;

                    _context.States.Update(existingEntity);
                    _logger.LogDebug("UPDATE State {Id}", entity.Id);
                }
                else
                {
                    _context.States.Add(entity);
                    _logger.LogWarning("State {Id} not found for UPDATE, inserting instead", entity.Id);
                }
                break;

            case "DELETE":
                var entityToDelete = await _context.States
                    .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

                if (entityToDelete != null)
                {
                    entityToDelete.Status = 0;
                    entityToDelete.UpdatedAt = entity.UpdatedAt;
                    entityToDelete.UpdatedBy = entity.UpdatedBy;

                    _context.States.Update(entityToDelete);
                    _logger.LogDebug("DELETE (soft) State {Id}", entity.Id);
                }
                else
                {
                    _logger.LogDebug("State {Id} not found for DELETE, skipping", entity.Id);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown operation: {operation}");
        }
    }
}
