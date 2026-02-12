using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IActivityService
    {
        Task LogAsync(string title, string description, string icon, string colorClass, string? link = null);
        Task<IEnumerable<ActivityDto>> GetRecentAsync(int count = 5, CancellationToken ct = default);
    }
}
