namespace abc.bvl.AdminTool.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<TResult> ExecuteAsync<TResult>(Func<IAdminDbContext, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<IAdminDbContext, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create(string dbRoute);
}