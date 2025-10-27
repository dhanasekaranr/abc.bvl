using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace abc.bvl.AdminTool.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ScreenDefinition entity mapping to Oracle database
/// </summary>
public class ScreenDefinitionConfiguration : IEntityTypeConfiguration<ScreenDefinition>
{
    public void Configure(EntityTypeBuilder<ScreenDefinition> builder)
    {
        // Map to Oracle table in APP_USER schema (Oracle stores identifiers in uppercase)
        builder.ToTable("ADMIN_SCREENDEFN", "APP_USER");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("SCREENDEFNID")
            .ValueGeneratedOnAdd();

        // Business Properties
        builder.Property(x => x.Name)
            .HasColumnName("SCREENNAME")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("SCREENCODE")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("SCREENDESC")
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .HasColumnName("DISPLAYORDER")
            .HasDefaultValue(0);

        builder.Property(x => x.Status)
            .HasColumnName("STATUS")
            .HasColumnType("NUMBER(3)")
            .IsRequired();

        // Audit Properties
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATEDAT")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CREATEDBY")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATEDAT")
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("UPDATEDBY")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnName("ROWVERSION")
            .HasMaxLength(50)
            .IsConcurrencyToken();

        // Indexes
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UK_ADMIN_SCREENDEFN_CODE");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_ADMIN_SCREENDEFN_NAME");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ADMIN_SCREENDEFN_STATUS");

        // Relationships
        builder.HasMany(x => x.ScreenPilots)
            .WithOne(x => x.ScreenDefinition)
            .HasForeignKey(x => x.ScreenDefnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}