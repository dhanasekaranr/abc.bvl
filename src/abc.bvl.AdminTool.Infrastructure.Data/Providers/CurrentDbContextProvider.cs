using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;

namespace abc.bvl.AdminTool.Infrastructure.Data.Providers;

/// <summary>
/// Request-scoped provider for database context routing
/// Selects between Primary and Secondary database contexts based on middleware configuration
/// </summary>
public class CurrentDbContextProvider : ICurrentDbContextProvider
{
    private readonly AdminDbPrimaryContext _primaryContext;
    private readonly AdminDbSecondaryContext _secondaryContext;
    private DatabaseContextType _contextType = DatabaseContextType.Primary;

    public CurrentDbContextProvider(
        AdminDbPrimaryContext primaryContext,
        AdminDbSecondaryContext secondaryContext)
    {
        _primaryContext = primaryContext;
        _secondaryContext = secondaryContext;
    }

    public AdminDbContext GetContext()
    {
        return _contextType == DatabaseContextType.Primary
            ? _primaryContext
            : _secondaryContext;
    }

    public void SetContextType(DatabaseContextType contextType)
    {
        _contextType = contextType;
    }
}
