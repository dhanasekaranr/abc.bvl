using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace abc.bvl.AdminTool.Infrastructure.Data.Configurations;

public class ScreenDefinitionConfiguration : IEntityTypeConfiguration<ScreenDefinition>
{
    public void Configure(EntityTypeBuilder<ScreenDefinition> builder)
    {
        builder.ToTable("ADMIN_SCREENDEFN", "APP_USER");
        builder.HasKey(x => x.ScreenGk);
        builder.Property(x => x.ScreenGk).HasColumnName("SCREEN_GK").ValueGeneratedNever();
        builder.Property(x => x.ScreenName).HasColumnName("SCREENNAME").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("STATUSID").HasColumnType("NUMBER(1)").IsRequired();
        builder.Property(x => x.CreatedDt).HasColumnName("CREATEDDT").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasColumnType("NUMBER(9)").IsRequired();
        builder.Property(x => x.UpdatedDt).HasColumnName("UPDATEDDT").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("UPDATEDBY").HasColumnType("NUMBER(9)").IsRequired();
        builder.HasMany(x => x.ScreenPilots).WithOne(x => x.ScreenDefinition).HasForeignKey(x => x.ScreenGk).OnDelete(DeleteBehavior.Cascade);
    }
}
