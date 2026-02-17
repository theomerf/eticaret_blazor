namespace Infrastructure.BackgroundJobs.Hangfire
{
    public sealed class HangfireOptions
    {
        public bool Enabled { get; set; } = true;

        public string ConnectionStringName { get; set; } = "postgresqlconnection";

        public string SchemaName { get; set; } = "hangfire";

        public int WorkerCount { get; set; } = Math.Max(1, Environment.ProcessorCount);

        public string[] QueueNames { get; set; } = Queues.All;

        public string ServerNamePrefix { get; set; } = "ETicaret";
    }
}
