using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IActivityService
    {
        Task LogActivityAsync(string title, string description, string icon, string colorClass, string? link = null);
        Task<IEnumerable<ActivityDto>> GetRecentActivitiesAsync(int count = 5);
    }
}
