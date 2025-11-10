using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Context;

/// <summary>
/// Primary database context using APP_USER schema
/// Used for standard application operations
/// </summary>
public class AdminDbPrimaryContext : AdminDbContext
{
    public AdminDbPrimaryContext(DbContextOptions<AdminDbPrimaryContext> options) : base(options)
    {
    }

}
