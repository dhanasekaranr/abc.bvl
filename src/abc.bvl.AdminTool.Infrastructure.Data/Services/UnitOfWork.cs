using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AdminDbContext _context;

    public UnitOfWork(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<IAdminDbContext, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(_context, cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<IAdminDbContext, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(_context, cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}