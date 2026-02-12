using Application.DTOs;

namespace Application.Services.Interfaces
{
    public interface ICaptchaService
    {
        Task<bool> ValidateAsync(string? token);
        Task<CaptchaResponseDto> VerifyAsync(string? token);
    }
}
