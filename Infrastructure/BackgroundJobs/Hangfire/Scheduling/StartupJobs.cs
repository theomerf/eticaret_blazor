using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs.Hangfire.Scheduling
{
    public static class StartupJobs
    {
        public static void Register(IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Hangfire.StartupJobs");

            RecurringJobRegistrar.Register(recurringJobManager, configuration);
            logger.LogInformation("Hangfire recurring jobs registered.");
        }
    }
}
