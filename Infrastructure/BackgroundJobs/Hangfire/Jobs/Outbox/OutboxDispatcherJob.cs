namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Outbox
{
    public class OutboxDispatcherJob
    {
        // Placeholder for future outbox integration.
        // TODO: Dispatch integration events from outbox table.
        public Task ExecuteAsync(CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
