using Hangfire;
using Hangfire.PostgreSql;
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

            return services;
        }
    }
}
