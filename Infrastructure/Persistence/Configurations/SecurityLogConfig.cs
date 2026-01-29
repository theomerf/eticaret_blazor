using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SecurityLogConfig : IEntityTypeConfiguration<SecurityLog>
    {
        public void Configure(EntityTypeBuilder<SecurityLog> builder)
        {
            builder.HasKey(s => s.SecurityLogId);

            builder.Property(s => s.EventType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.UserId)
                .HasMaxLength(450);

            builder.Property(s => s.UserName)
                .HasMaxLength(256);

            builder.Property(s => s.Email)
                .HasMaxLength(256);

            builder.Property(s => s.IpAddress)
                .IsRequired()
                .HasMaxLength(45);

            builder.Property(s => s.UserAgent)
                .HasMaxLength(500);

            builder.Property(s => s.FailureReason)
                .HasMaxLength(500);

            builder.HasIndex(s => s.EventType)
                .HasDatabaseName("IX_SecurityLogs_EventType");

            builder.HasIndex(s => s.UserId)
                .HasDatabaseName("IX_SecurityLogs_UserId");

            builder.HasIndex(s => s.Timestamp)
                .IsDescending()
                .HasDatabaseName("IX_SecurityLogs_Timestamp_Desc");

            builder.HasIndex(s => new { s.EventType, s.IsSuccess })
                .HasDatabaseName("IX_SecurityLogs_Event_Success");
        }
    }
}
