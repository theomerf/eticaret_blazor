using Application.DTOs;

namespace Application.Services.Interfaces
{
    public interface ISystemService
    {
        Task<SystemStatusDto> GetSystemStatusAsync();
    }
}
