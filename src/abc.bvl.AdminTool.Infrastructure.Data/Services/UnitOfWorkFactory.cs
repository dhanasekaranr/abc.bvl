using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork Create(string dbRoute)
    {
        // For now, we'll use the same context regardless of route
        // In a full implementation, you'd select Primary or Secondary based on dbRoute
        var context = _serviceProvider.GetRequiredService<AdminDbContext>();
        return new UnitOfWork(context);
    }
}