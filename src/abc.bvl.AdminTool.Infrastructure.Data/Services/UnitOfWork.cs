using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

/// <summary>
/// Unit of Work implementation that resolves DbContext dynamically
/// Uses DbContextResolver to get the appropriate database (Primary/Secondary) based on request context
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly Func<AdminDbContext> _contextFactory;

    /// <summary>
    /// Constructor for dependency injection with DbContextResolver (recommended)
    /// </summary>
    public UnitOfWork(Func<AdminDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<IAdminDbContext, CancellationToken, Task<TResult>> operation, 
        CancellationToken cancellationToken = default)
    {
        // Resolve DbContext at execution time (supports dynamic routing)
        var context = _contextFactory();
        
        await context.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(context, cancellationToken);
            await context.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteAsync(
        Func<IAdminDbContext, CancellationToken, Task> operation, 
        CancellationToken cancellationToken = default)
    {
        // Resolve DbContext at execution time (supports dynamic routing)
        var context = _contextFactory();
        
        await context.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(context, cancellationToken);
            await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
