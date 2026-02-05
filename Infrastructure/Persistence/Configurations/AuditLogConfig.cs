using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(al => al.AuditLogId);

            builder.Property(al => al.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(al => al.UserName)
                .HasMaxLength(256);

            builder.Property(al => al.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(al => al.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(al => al.EntityId)
                .HasMaxLength(100);

            builder.Property(al => al.IpAddress)
                .IsRequired()
                .HasMaxLength(45);

            builder.Property(al => al.UserAgent)
                .HasMaxLength(500);

            builder.Property(al => al.OldValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(al => al.NewValues)
                .HasColumnType("nvarchar(max)");

            builder.HasIndex(al => al.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            builder.HasIndex(al => al.EntityName)
                .HasDatabaseName("IX_AuditLogs_EntityName");

            builder.HasIndex(a => a.Timestamp)
                .IsDescending()
                .HasDatabaseName("IX_AuditLogs_Timestamp_Desc");

            builder.HasIndex(a => new { a.EntityName, a.EntityId })
                .HasDatabaseName("IX_AuditLogs_Entity");
        }
    }
}
