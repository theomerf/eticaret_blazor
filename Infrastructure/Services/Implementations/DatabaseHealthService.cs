using Application.Services.Interfaces;
using Infrastructure.Persistence;
using System.Diagnostics;

namespace Infrastructure.Services.Implementations
{
    public class DatabaseHealthService : IDatabaseHealthService
    {
        private readonly RepositoryContext _db;

        public DatabaseHealthService(RepositoryContext db)
        {
            _db = db;
        }

        public async Task<long?> CheckAsync()
        {
            var sw = Stopwatch.StartNew();

            var canConnect = await _db.Database.CanConnectAsync();
            sw.Stop();

            return canConnect ? sw.ElapsedMilliseconds : null;
        }
    }

}
