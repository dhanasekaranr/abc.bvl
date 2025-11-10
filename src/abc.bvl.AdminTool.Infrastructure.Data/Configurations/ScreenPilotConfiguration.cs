using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace abc.bvl.AdminTool.Infrastructure.Data.Configurations;

public class ScreenPilotConfiguration : IEntityTypeConfiguration<ScreenPilot>
{
    public void Configure(EntityTypeBuilder<ScreenPilot> builder)
    {
        builder.ToTable("ADMIN_SCREENPILOT", "APP_USER");
        builder.HasKey(x => x.ScreenPilotGk);
        builder.Property(x => x.ScreenPilotGk).HasColumnName("SCREENPILOT_GK").ValueGeneratedNever();
        builder.Property(x => x.NbUserGk).HasColumnName("NBUSER_GK").HasColumnType("NUMBER(9)").IsRequired();
        builder.Property(x => x.ScreenGk).HasColumnName("SCREEN_GK").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("STATUSID").HasColumnType("NUMBER(1)").IsRequired();
        builder.Property(x => x.DualMode).HasColumnName("DUALMODE").HasColumnType("NUMBER(1)").IsRequired();
        builder.Property(x => x.CreatedDt).HasColumnName("CREATEDDT").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasColumnType("NUMBER(9)").IsRequired();
        builder.Property(x => x.UpdatedDt).HasColumnName("UPDATEDDT").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("UPDATEDBY").HasColumnType("NUMBER(9)").IsRequired();
        builder.HasIndex(x => new { x.NbUserGk, x.ScreenGk }).IsUnique().HasDatabaseName("UK_SCREENPILOT_USER_SCREEN");
        builder.HasOne(x => x.ScreenDefinition).WithMany(x => x.ScreenPilots).HasForeignKey(x => x.ScreenGk).OnDelete(DeleteBehavior.Cascade);
    }
}
