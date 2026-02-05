namespace Application.DTOs
{
    public class SystemStatusDto
    {
        public string ServerStatus { get; set; } = null!;
        public string Uptime { get; set; } = null!;
        public string DbStatus { get; set; } = null!;
        public string DbResponseTime { get; set; } = null!;
        public string MemoryUsageRatio { get; set; } = null!;
        public string MemoryDetails { get; set; } = null!;
        public string DiskUsageRatio { get; set; } = null!;
        public string DiskDetails { get; set; } = null!;
    }
}
