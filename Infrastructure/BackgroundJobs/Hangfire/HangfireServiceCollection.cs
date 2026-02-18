using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Email;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Activity;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Maintenance;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Notifications;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Orders;
using Infrastructure.BackgroundJobs.Hangfire.Jobs.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundJobs.Hangfire
{
    public static class HangfireServiceCollection
    {
        public static IServiceCollection AddHangfireInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = configuration
                .GetSection("Hangfire")
                .Get<HangfireOptions>() ?? new HangfireOptions();

            if (!options.Enabled)
                return services;

            services.Configure<SoftDeleteCleanupOptions>(configuration.GetSection("Hangfire:SoftDeleteCleanup"));

            var connStr = configuration.GetConnectionString(options.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("Hangfire connection string bulunamadı.");

            services.AddHangfire((sp, hf) =>
            {
                hf.UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(
                      bootstrapper =>
                      {
                          bootstrapper.UseNpgsqlConnection(connStr);
                      },
                      new PostgreSqlStorageOptions
                      {
                          SchemaName = options.SchemaName
                      });
            });

            services.AddHangfireServer(o =>
            {
                o.Queues = options.QueueNames.Distinct().ToArray();
                o.WorkerCount = options.WorkerCount;
                o.ServerName = $"{options.ServerNamePrefix}-{Environment.MachineName}";
            });

            services.AddTransient<EmailJob>();
            services.AddTransient<NotificationDispatchJob>();
            services.AddTransient<NotificationCreateJob>();
            services.AddTransient<ActivityLogJob>();
            services.AddTransient<SoftDeleteCleanupJob>();
            services.AddTransient<PaymentPendingTimeoutJob>();
            services.AddTransient<OutboxDispatcherJob>();
            services.AddScoped<EmailQueueService>();
            services.AddScoped<NotificationQueueService>();
            services.AddScoped<ActivityQueueService>();

            return services;
        }
    }
}
