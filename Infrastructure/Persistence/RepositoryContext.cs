using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence
{
    public class RepositoryContext : IdentityDbContext<User, Role, string, IdentityUserClaim<string>, UserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
        public DbSet<UserReview> UserReviews { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartLine> CartLines { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<OrderCampaign> OrderCampaigns { get; set; }
        public DbSet<OrderHistory> OrderHistories { get; set; }
        public DbSet<OrderLinePaymentTransaction> OrderLinePaymentTransactions { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<CategoryVariantAttribute> CategoryVariantAttributes { get; set; }
        public DbSet<UserReviewVote> UserReviewVotes { get; set; }

        public RepositoryContext(DbContextOptions<RepositoryContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
