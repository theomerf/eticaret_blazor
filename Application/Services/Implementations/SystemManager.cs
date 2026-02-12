using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using System.Diagnostics;

namespace Application.Services.Implementations
{
    public class SystemManager : ISystemService
    {
        private readonly IDatabaseHealthService _databaseHealthService;

        public SystemManager(IDatabaseHealthService databaseHealthService)
        {
            _databaseHealthService = databaseHealthService;
        }

        public async Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken ct = default)
        {
            var status = new SystemStatusDto();

            // Sunucu çalışma süresi
            try
            {
                using var process = Process.GetCurrentProcess();
                var uptime = DateTime.Now - process.StartTime;
                
                var parts = new List<string>();
                
                if (uptime.Days > 0) parts.Add($"{uptime.Days}g");
                if (uptime.Hours > 0) parts.Add($"{uptime.Hours}s");
                if (uptime.Minutes > 0) parts.Add($"{uptime.Minutes}dk");
                
                if (parts.Count == 0)
                {
                    parts.Add($"{uptime.Seconds}sn");
                }

                status.Uptime = string.Join(" ", parts);
                status.ServerStatus = "Aktif";
            }
            catch
            {
                status.Uptime = "Bilinmiyor";
            }

            // Veritabanı durumu
            try
            {
                var elapsed = await _databaseHealthService.CheckAsync(ct);

                if (elapsed is null)
                {
                    status.DbStatus = "Hata";
                    status.DbResponseTime = "-";
                }
                else
                {
                    status.DbResponseTime = $"{elapsed}ms";

                    if (elapsed < 200) status.DbStatus = "Mükemmel";
                    else if (elapsed < 500) status.DbStatus = "Sağlıklı";
                    else if (elapsed < 1000) status.DbStatus = "Yavaş";
                    else status.DbStatus = "Kritik";
                }
            }
            catch
            {
                status.DbStatus = "Hata";
                status.DbResponseTime = "-";
            }

            // Bellek kullanımı
            try
            {
                using var process = Process.GetCurrentProcess();
                long usedMemory = process.WorkingSet64;
                
                var gcInfo = GC.GetGCMemoryInfo();
                long totalMemory = gcInfo.TotalAvailableMemoryBytes;

                if (totalMemory <= 0) totalMemory = 8L * 1024 * 1024 * 1024;

                double usedGb = usedMemory / (1024.0 * 1024 * 1024);
                double totalGb = totalMemory / (1024.0 * 1024 * 1024);
                int ratio = (int)((usedMemory * 100) / totalMemory);
                
                if (ratio == 0 && usedMemory > 0) ratio = 1;

                status.MemoryUsageRatio = $"%{ratio}";
                status.MemoryDetails = $"{usedGb:F2}GB / {totalGb:F0}GB";
            }
            catch
            {
                status.MemoryUsageRatio = "-";
                status.MemoryDetails = "Hesaplanamadı";
            }

            // Disk kullanımı
            try
            {
                var driveInfo = new DriveInfo(Directory.GetCurrentDirectory());
                if (driveInfo.IsReady)
                {
                    long totalSize = driveInfo.TotalSize;
                    long totalFree = driveInfo.AvailableFreeSpace;
                    long used = totalSize - totalFree;

                    double usedGb = used / (1024.0 * 1024 * 1024);
                    double totalGb = totalSize / (1024.0 * 1024 * 1024);
                    int ratio = (int)((used * 100) / totalSize);

                    status.DiskUsageRatio = $"%{ratio}";
                    status.DiskDetails = $"{usedGb:F0}GB / {totalGb:F0}GB";
                }
            }
            catch
            {
                status.DiskUsageRatio = "-";
                status.DiskDetails = "Erişilemedi";
            }

            return status;
        }
    }
}
