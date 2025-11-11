using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;

namespace abc.bvl.AdminTool.Benchmarks.Base
{
    /// <summary>
    /// Simple implementation for benchmarks: always returns the provided context
    /// </summary>
    public class BenchmarkDbContextProvider : ICurrentDbContextProvider
    {
        private readonly AdminDbContext _context;
        public BenchmarkDbContextProvider(AdminDbContext context)
        {
            _context = context;
        }
        public AdminDbContext GetContext() => _context;
    public void SetContextType(abc.bvl.AdminTool.Infrastructure.Data.Interfaces.DatabaseContextType contextType) { /* no-op for benchmarks */ }
    }
}
