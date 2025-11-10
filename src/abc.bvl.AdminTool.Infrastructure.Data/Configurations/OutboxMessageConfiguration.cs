using abc.bvl.AdminTool.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace abc.bvl.AdminTool.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for OutboxMessage entity mapping to Oracle database
/// Implements the Transactional Outbox pattern for cross-database consistency
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // Map to Oracle table in default schema
        builder.ToTable("CVLWEBTOOLS_ADMINTOOLOUTBOX");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("OUTBOXID")
            .ValueGeneratedOnAdd();

        // Entity Identification
        builder.Property(x => x.Type)
            .HasColumnName("ENTITYTYPE")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("ENTITYID")
            .IsRequired();

        builder.Property(x => x.Operation)
            .HasColumnName("OPERATION")
            .HasMaxLength(20)
            .IsRequired();

        // Payload
        builder.Property(x => x.Payload)
            .HasColumnName("PAYLOAD")
            .HasColumnType("CLOB")
            .IsRequired();

        // Processing Information
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATEDAT")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("PROCESSEDAT");

        builder.Property(x => x.Status)
            .HasColumnName("STATUS")
            .HasMaxLength(20)
            .HasDefaultValue("Pending")
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasColumnName("RETRYCOUNT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasColumnName("ERRORMESSAGE")
            .HasMaxLength(4000);

        // Routing Information
        builder.Property(x => x.SourceDatabase)
            .HasColumnName("SOURCEDATABASE")
            .HasMaxLength(50);

        builder.Property(x => x.TargetDatabase)
            .HasColumnName("TARGETDATABASE")
            .HasMaxLength(50);

        builder.Property(x => x.CorrelationId)
            .HasColumnName("CORRELATIONID")
            .HasMaxLength(100);

        // Indexes for efficient outbox processing
        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_OUTBOX_STATUS_CREATED");

        builder.HasIndex(x => new { x.Type, x.EntityId })
            .HasDatabaseName("IX_OUTBOX_ENTITY");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_OUTBOX_CREATEDAT");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_OUTBOX_CORRELATIONID");
    }
}