using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ETicaret
{
    public sealed class RepositoryContextDesignTimeFactory : IDesignTimeDbContextFactory<RepositoryContext>
    {
        public RepositoryContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = configuration.GetConnectionString("postgresqlconnection_migrations");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'postgresqlconnection' was not found.");

            var optionsBuilder = new DbContextOptionsBuilder<RepositoryContext>();

            optionsBuilder.UseNpgsql(cs, npgsql =>
            {
                npgsql.MigrationsAssembly("ETicaret");
                npgsql.CommandTimeout(180);
                npgsql.EnableRetryOnFailure(maxRetryCount: 5);
            });

            return new RepositoryContext(optionsBuilder.Options);
        }
    }
}
