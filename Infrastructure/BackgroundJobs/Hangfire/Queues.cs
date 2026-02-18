namespace Infrastructure.BackgroundJobs.Hangfire
{
    public static class Queues
    {
        public const string Critical = "critical";
        public const string Orders = "orders";
        public const string Notifications = "notifications";
        public const string Outbox = "outbox";
        public const string Default = "default";
        public const string Maintenance = "maintenance";
        public const string Low = "low";

        public static readonly string[] All = new[] { Critical, Orders, Notifications, Outbox, Default, Maintenance, Low };
    }
}
