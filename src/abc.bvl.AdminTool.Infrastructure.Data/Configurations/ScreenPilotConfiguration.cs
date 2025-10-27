using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace abc.bvl.AdminTool.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ScreenPilot entity mapping to Oracle database
/// </summary>
public class ScreenPilotConfiguration : IEntityTypeConfiguration<ScreenPilot>
{
    public void Configure(EntityTypeBuilder<ScreenPilot> builder)
    {
        // Map to Oracle table in APP_USER schema
        builder.ToTable("ADMIN_SCREENPILOT", "APP_USER");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("SCREENPILOTID")
            .ValueGeneratedOnAdd();

        // Business Properties
        builder.Property(x => x.ScreenDefnId)
            .HasColumnName("SCREENDEFNID")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("USERID")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("STATUS")
            .HasColumnType("NUMBER(3)")
            .IsRequired();

        builder.Property(x => x.AccessLevel)
            .HasColumnName("ACCESSLEVEL")
            .HasMaxLength(50);

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
        builder.HasIndex(x => new { x.ScreenDefnId, x.UserId })
            .IsUnique()
            .HasDatabaseName("UK_ADMIN_SCREENPILOT");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_ADMIN_SCREENPILOT_USER");

        builder.HasIndex(x => x.ScreenDefnId)
            .HasDatabaseName("IX_ADMIN_SCREENPILOT_SCREEN");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ADMIN_SCREENPILOT_STATUS");

        // Relationships
        builder.HasOne(x => x.ScreenDefinition)
            .WithMany(x => x.ScreenPilots)
            .HasForeignKey(x => x.ScreenDefnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}