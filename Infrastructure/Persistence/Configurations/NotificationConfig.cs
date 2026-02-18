using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.NotificationId);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(n => n.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(n => n.DeletedByUserId)
                   .HasMaxLength(450);

            builder.Property(n => n.IsSystemGenerated)
                .HasDefaultValue(true);

            builder.Property(n => n.IsSent)
                .HasDefaultValue(false);

            builder.Property(n => n.NotificationGroupId)
                .HasMaxLength(64);

            builder.Property(n => n.SentToAllActiveUsers)
                .HasDefaultValue(false);

            builder.Property(p => p.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(p => p.UpdatedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(n => !n.IsDeleted);

            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            builder.HasIndex(n => new { n.UserId, n.IsRead, n.IsDeleted })
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Notifications_User_Unread");

            builder.HasIndex(n => new { n.ScheduledFor, n.IsSent })
                .HasFilter("\"ScheduledFor\" IS NOT NULL AND \"IsSent\" = false")
                .HasDatabaseName("IX_Notifications_Scheduled");

            builder.HasIndex(n => n.NotificationGroupId)
                .HasFilter("\"NotificationGroupId\" IS NOT NULL")
                .HasDatabaseName("IX_Notifications_GroupId");

            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
