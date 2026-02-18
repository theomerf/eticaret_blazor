using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;
using System.Security.Claims;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(AuthRequest authenticateDto);
        Task<IList<Claim>> BuildClaimsAsync(string userId);
        Task<RegisterResult> RegisterAsync(RegisterRequest register, CancellationToken ct = default);
        Task<ConfirmEmailResult> ConfirmEmailAsync(string userId, string token);
        Task LogoutAsync(string userId, string userName);

        Task<OperationResult<UserDto>> ResetPasswordAsync(ResetPasswordDto model);
        Task<OperationResult<UserDto>> ChangePasswordAsync(ChangePasswordDto model);

        Task<OperationResult<UserDto>> VerifyEmailAsync(string userId);
        Task<OperationResult<string>> GeneratePasswordResetLinkAsync(string userId);

        Task<ImpersonationResult> StartImpersonationAsync(string targetUserId, string adminId);
        Task<ImpersonationResult> StopImpersonationAsync(string originalAdminId);
    }
}
