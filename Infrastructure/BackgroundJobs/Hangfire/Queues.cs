namespace Infrastructure.BackgroundJobs.Hangfire
{
    public static class Queues
    {
        public const string Critical = "critical";
        public const string Default = "default";
        public const string Low = "low";

        public static readonly string[] All = new[] { Critical, Default, Low };
    }
}
