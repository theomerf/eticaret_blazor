using Application.Common.Exceptions;
using Application.Common.Models;
using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class AuthManager : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ICacheService _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<AuthManager> _logger;
        private readonly IRepositoryManager _manager;
        private readonly ISecurityLogService _securityLogService;
        private readonly ICaptchaService _captchaService;
        private readonly IEmailQueueService _emailQueueService;

        public AuthManager(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ICacheService cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            INotificationQueueService notificationQueueService,
            ILogger<AuthManager> logger,
            IRepositoryManager manager,
            ISecurityLogService securityLogService,
            ICaptchaService captchaService,
            IEmailQueueService emailQueueService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _notificationQueueService = notificationQueueService;
            _logger = logger;
            _manager = manager;
            _securityLogService = securityLogService;
            _captchaService = captchaService;
            _emailQueueService = emailQueueService;
        }

        public async Task<AuthResult> AuthenticateAsync(AuthRequest authenticate)
        {
            if (!authenticate.SkipCaptcha && !await _captchaService.ValidateAsync(authenticate.CaptchaToken))
                return AuthResult.Failure_CaptchaFailed();

            if (await _securityLogService.IsIpBlockedAsync(authenticate.IpAddress))
            {
                await _securityLogService.LogFailedLoginAsync(
                    email: authenticate.Email,
                    failureReason: "IP blocked due to too many failed attempts");

                return AuthResult.Failure_IpBlocked();
            }

            var user = await _userManager.FindByEmailAsync(authenticate.Email);

            if (user is null || user.Email is null || user.UserName is null)
            {
                await _securityLogService.LogFailedLoginAsync(
                    email: authenticate.Email,
                    failureReason: "Invalid credentials");

                return AuthResult.Failure_InvalidCredentials();
            }

            if (!user.EmailConfirmed)
                return AuthResult.Failure_EmailNotConfirmed();

            if (user.IsDeleted)
                return AuthResult.Failure_AccountDeleted();

            if (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                var remaining = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                return AuthResult.Failure_LockedOut((int)remaining.TotalMinutes);
            }

            await _signInManager.SignOutAsync();

            var result = await _signInManager.PasswordSignInAsync(
                authenticate.Email,
                authenticate.Password,
                authenticate.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await UpdateUserOnSuccessfulLoginAsync(user, authenticate.IpAddress);

                await _securityLogService.LogLoginAsync(
                    userId: user.Id,
                    userName: user.UserName!,
                    email: user.Email!,
                    isSuccess: true);

                return AuthResult.Success(user.Id, user.UserName!, user.Email!);
            }

            if (result.IsLockedOut)
            {
                await _securityLogService.LogFailedLoginAsync(
                    email: authenticate.Email,
                    failureReason: "Account locked out");

                return AuthResult.Failure_LockedOut(remainingMinutes: 0);
            }

            user.LastFailedLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _securityLogService.LogFailedLoginAsync(
                email: authenticate.Email,
                failureReason: "Invalid credentials");

            return AuthResult.Failure_InvalidCredentials();
        }

        public async Task<IList<Claim>> BuildClaimsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UserNotFoundException(userId);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name,           user.UserName!),
                new(ClaimTypes.NameIdentifier, user.Id),
                new("first_name",              user.FirstName),
                new("last_name",               user.LastName),
                new("identity_number",         user.IdentityNumber)
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            return claims;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequest register, CancellationToken ct = default)
        {
            RegisterResult? finalResult = null;

            try
            {
                await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    if (!register.SkipCaptcha && !await _captchaService.ValidateAsync(register.CaptchaToken))
                        throw new RegistrationException(RegisterResult.Failure_CaptchaFailed());

                    var user = new User
                    {
                        UserName = register.RegisterDto.Email,
                        Email = register.RegisterDto.Email,
                        FirstName = register.RegisterDto.FirstName,
                        LastName = register.RegisterDto.LastName,
                        PhoneNumber = register.RegisterDto.PhoneNumber,
                        BirthDate = register.RegisterDto.BirthDate,
                        RegistrationIpAddress = register.IpAddress,
                        AcceptedTerms = register.RegisterDto.AcceptTerms,
                        TermsAcceptedDate = DateTime.UtcNow,
                        AcceptedMarketing = register.RegisterDto.AcceptMarketing,
                        MarketingAcceptedDate = register.RegisterDto.AcceptMarketing ? DateTime.UtcNow : null
                    };

                    var createResult = await _userManager.CreateAsync(user, register.RegisterDto.Password);

                    if (!createResult.Succeeded)
                        throw new RegistrationException(
                            RegisterResult.Failure_ValidationErrors(createResult.Errors.Select(e => e.Description)));

                    var roleResult = await _userManager.AddToRoleAsync(user, "User");

                    if (!roleResult.Succeeded)
                        throw new RegistrationException(
                            RegisterResult.Failure_ValidationErrors(roleResult.Errors.Select(e => e.Description)));

                    var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = register.ConfirmationLinkTemplate
                        .Replace("__USER_ID__", Uri.EscapeDataString(user.Id))
                        .Replace("__TOKEN__", Uri.EscapeDataString(emailToken));

                    finalResult = RegisterResult.Success(user.Id, user.Email!, confirmationLink);
                    return finalResult;
                }, ct: ct);
            }
            catch (RegistrationException ex)
            {
                return ex.Result;
            }

            if (finalResult?.Succeeded == true)
                _emailQueueService.EnqueueConfirmationEmail(finalResult.Email!, finalResult.ConfirmationLink!);

            return finalResult!;
        }

        public async Task<ConfirmEmailResult> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return ConfirmEmailResult.Failure_UserNotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded || user.Email is null)
                return ConfirmEmailResult.Failure_InvalidToken();

            _emailQueueService.EnqueueWelcomeEmail(user.Email, user.FirstName);

            return ConfirmEmailResult.Success(user.Email, user.FirstName);
        }

        public async Task LogoutAsync(string userId, string userName)
        {
            await _securityLogService.LogLogoutAsync(userId, userName);
        }

        public async Task<User> GetOneUserForServiceAsync(string userId, bool trackChanges, CancellationToken ct = default)
        {
            var user = await _manager.User.GetByIdAsync(userId, trackChanges, ct);
            if (user == null)
                throw new UserNotFoundException(userId);

            return user;
        }

        public async Task<OperationResult<UserDto>> ResetPasswordAsync(ResetPasswordDto model)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var user = await GetOneUserForServiceAsync(userId, false);

            if (model.NewPassword == model.ConfirmPassword)
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Password reset failed for user {UserId}", userId);
                    return OperationResult<UserDto>.Failure("Şifre sıfırlanamadı.", ResultType.ValidationError);
                }
            }
            else
            {
                _logger.LogWarning("Password mismatch during reset for user {UserId}", userId);
                return OperationResult<UserDto>.Failure("Yeni şifre ve onay şifresi eşleşmiyor.", ResultType.ValidationError);
            }

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "ResetPassword",
                entityName: "User",
                entityId: user.Id
            );

            _logger.LogInformation("Password reset successfully for user {UserId}", userId);
            return OperationResult<UserDto>.Success("Şifre başarıyla sıfırlandı.");
        }

        public async Task<OperationResult<UserDto>> ChangePasswordAsync(ChangePasswordDto model)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var user = await GetOneUserForServiceAsync(userId, false);

            if (model.NewPassword == model.ConfirmPassword)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.Password, model.NewPassword);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Password change failed for user {UserId}", userId);
                    return OperationResult<UserDto>.Failure("Şifre güncellenemedi.", ResultType.ValidationError);
                }
            }
            else
            {
                _logger.LogWarning("Password mismatch during change for user {UserId}", userId);
                return OperationResult<UserDto>.Failure("Yeni şifre ve onay şifresi eşleşmiyor.", ResultType.ValidationError);
            }

            _notificationQueueService.EnqueueCreate(new NotificationDtoForCreation
            {
                NotificationType = NotificationType.Settings,
                Title = "Şifreniz güncellendi",
                Description = $"Şifreniz {DateTime.Now} tarihinde güncellendi.",
                UserId = userId
            });

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "ChangePassword",
                entityName: "User",
                entityId: user.Id
            );

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return OperationResult<UserDto>.Success("Şifre başarıyla güncellendi.");
        }

        public async Task<OperationResult<UserDto>> VerifyEmailAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId, true);
            var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            if (user.EmailConfirmed)
            {
                return OperationResult<UserDto>.Failure("E-posta zaten doğrulanmış.", ResultType.ValidationError);
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Email verification failed for user {UserId}. Errors: {Errors}", userId, errors);
                return OperationResult<UserDto>.Failure($"E-posta doğrulanamadı: {errors}", ResultType.ValidationError);
            }

            await _auditLogService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: "VerifyEmail",
                entityName: "User",
                entityId: userId
            );

            _logger.LogInformation("Email verified by admin for user {UserId}. Admin: {AdminId}", userId, adminId);
            return OperationResult<UserDto>.Success("E-posta başarıyla doğrulandı.");
        }

        public async Task<OperationResult<string>> GeneratePasswordResetLinkAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId, false);
            var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            var resetLink = $"{baseUrl}/reset-password?userId={userId}&token={encodedToken}";

            await _auditLogService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: "GeneratePasswordResetLink",
                entityName: "User",
                entityId: userId
            );

            _logger.LogInformation("Password reset link generated by admin for user {UserId}. Admin: {AdminId}", userId, adminId);
            return OperationResult<string>.Success(resetLink, "Şifre sıfırlama linki oluşturuldu.");
        }

        public async Task<ImpersonationResult> StartImpersonationAsync(string targetUserId, string adminId)
        {
            if (adminId == targetUserId)
                return ImpersonationResult.Failure_CannotImpersonateSelf();

            var adminUser = await _userManager.FindByIdAsync(adminId);
            if (adminUser is null)
                return ImpersonationResult.Failure_UserNotFound();

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser is null)
                return ImpersonationResult.Failure_UserNotFound();

            if (targetUser.IsDeleted)
                return ImpersonationResult.Failure_UserDeleted();

            if (!targetUser.IsActive)
                return ImpersonationResult.Failure_UserInactive();

            var userClaims = await BuildClaimsAsync(targetUserId);

            var impersonationClaims = userClaims.ToList();
            impersonationClaims.Add(new Claim("IsImpersonating", "true"));
            impersonationClaims.Add(new Claim("OriginalAdminId", adminId));

            await _securityLogService.LogImpersonationStartAsync(
                adminId: adminId,
                adminUserName: adminUser.UserName!,
                targetUserId: targetUserId,
                targetUserName: targetUser.UserName!);

            await _cache.RemoveByPrefixAsync("users:");

            _logger.LogWarning(
                "IMPERSONATION: Admin {AdminId} ({AdminName}) started impersonating user {TargetUserId} ({TargetName})",
                adminId, adminUser.UserName, targetUserId, targetUser.UserName);

            return ImpersonationResult.Success(impersonationClaims);
        }

        public async Task<ImpersonationResult> StopImpersonationAsync(string originalAdminId)
        {
            var adminUser = await _userManager.FindByIdAsync(originalAdminId);
            if (adminUser is null)
                return ImpersonationResult.Failure_UserNotFound();

            var adminClaims = await BuildClaimsAsync(originalAdminId);

            await _securityLogService.LogImpersonationStopAsync(
                adminId: originalAdminId,
                adminUserName: adminUser.UserName!);

            await _cache.RemoveByPrefixAsync("users:");

            _logger.LogInformation(
                "IMPERSONATION ENDED: Admin {AdminId} ({AdminName}) stopped impersonation",
                originalAdminId, adminUser.UserName);

            return ImpersonationResult.Success(adminClaims);
        }

        // Helpers
        private async Task UpdateUserOnSuccessfulLoginAsync(User user, string ipAddress)
        {
            user.LastLoginDate = DateTime.UtcNow;
            user.LastLoginIpAddress = ipAddress;
            user.LastFailedLoginDate = null;

            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);
        }
    }
}
