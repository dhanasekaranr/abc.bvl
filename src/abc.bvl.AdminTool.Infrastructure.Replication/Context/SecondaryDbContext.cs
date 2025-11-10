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

        // Use shared configuration classes for mapping
        modelBuilder.ApplyConfiguration(new abc.bvl.AdminTool.Infrastructure.Data.Configurations.ScreenDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new abc.bvl.AdminTool.Infrastructure.Data.Configurations.ScreenPilotConfiguration());

        // Country
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("COUNTRY");
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
            entity.ToTable("STATE");
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
