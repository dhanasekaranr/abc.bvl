using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Context;

/// <summary>
/// Secondary database context using CVLWEBTOOLS schema
/// Used for replication and secondary database operations
/// </summary>
public class AdminDbSecondaryContext : AdminDbContext
{
    public AdminDbSecondaryContext(DbContextOptions<AdminDbSecondaryContext> options) : base(options)
    {
    }

}
