using abc.bvl.AdminTool.Domain.Entities;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;

/// <summary>
/// Service for replicating entities to secondary database
/// </summary>
public interface IEntityReplicator
{
    /// <summary>
    /// Replicate an entity operation to the secondary database
    /// </summary>
    Task ReplicateAsync(string entityType, string operation, string payload, CancellationToken cancellationToken);
}
