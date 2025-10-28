using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

/// <summary>
/// Factory for creating UnitOfWork instances with database routing support
/// NOTE: This is kept for backward compatibility but may not be needed 
/// since UnitOfWork now uses DbContextResolver automatically
/// </summary>
public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork Create(string dbRoute)
    {
        // Resolve the appropriate DbContext based on dbRoute
        var contextKey = dbRoute.Equals("Secondary", StringComparison.OrdinalIgnoreCase) 
            ? "Secondary" 
            : "Primary";

        // Return UnitOfWork with factory that resolves the correct keyed service
        return new UnitOfWork(() => 
            _serviceProvider.GetRequiredKeyedService<AdminDbContext>(contextKey));
    }
}
