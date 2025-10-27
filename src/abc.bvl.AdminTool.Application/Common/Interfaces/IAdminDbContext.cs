using abc.bvl.AdminTool.Domain.Entities;

namespace abc.bvl.AdminTool.Application.Common.Interfaces;

public interface IAdminDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task EnqueueOutboxAsync(string type, object payload, CancellationToken cancellationToken = default);
}