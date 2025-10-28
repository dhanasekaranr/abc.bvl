using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Context;

/// <summary>
/// DbContext for secondary database replication
/// </summary>
public class SecondaryDbContext : DbContext
{
    public SecondaryDbContext(DbContextOptions<SecondaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<ScreenDefinition> ScreenDefinitions => Set<ScreenDefinition>();
    public DbSet<ScreenPilot> ScreenPilots => Set<ScreenPilot>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply same configurations as primary database
        // Oracle uppercase naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entity.SetTableName(tableName.ToUpperInvariant());
            }

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(columnName.ToUpperInvariant());
                }
            }
        }

        // ScreenDefinition
        modelBuilder.Entity<ScreenDefinition>(entity =>
        {
            entity.ToTable("SCREENDEFN", "ADMIN");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("SCREENDEFNID");
            entity.Property(e => e.ScreenName).HasColumnName("SCREENNAME").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
            entity.Property(e => e.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CREATEDAT").IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATEDAT");
            entity.Property(e => e.UpdatedBy).HasColumnName("UPDATEDBY").HasMaxLength(100);
        });

        // ScreenPilot
        modelBuilder.Entity<ScreenPilot>(entity =>
        {
            entity.ToTable("SCREENPILOT", "ADMIN");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("SCREENPILOTID");
            entity.Property(e => e.UserId).HasColumnName("USERID").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ScreenDefnId).HasColumnName("SCREENDEFNID").IsRequired();
            entity.Property(e => e.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CREATEDAT").IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATEDAT");
            entity.Property(e => e.UpdatedBy).HasColumnName("UPDATEDBY").HasMaxLength(100);
        });

        // Country
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("COUNTRY", "ADMIN");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasColumnName("CODE").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).HasColumnName("NAME").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CREATEDAT").IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATEDAT");
            entity.Property(e => e.UpdatedBy).HasColumnName("UPDATEDBY").HasMaxLength(100);
        });

        // State
        modelBuilder.Entity<State>(entity =>
        {
            entity.ToTable("STATE", "ADMIN");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasColumnName("CODE").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).HasColumnName("NAME").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CountryId).HasColumnName("COUNTRYID").IsRequired();
            entity.Property(e => e.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CREATEDAT").IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UPDATEDAT");
            entity.Property(e => e.UpdatedBy).HasColumnName("UPDATEDBY").HasMaxLength(100);
        });
    }
}
