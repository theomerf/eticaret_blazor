using Application.Common.Exceptions;
using Application.DTOs;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class AuthManager : IAuthService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IActivityService _activityService;
        private readonly ILogger<AuthManager> _logger;

        public AuthManager(
            RoleManager<IdentityRole> roleManager,
            UserManager<User> userManager,
            IMapper mapper,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IActivityService activityService,
            ILogger<AuthManager> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDto = _mapper.Map<IEnumerable<UserDto>>(users);

            return userDto;
        }

        public async Task<IEnumerable<IdentityRole>> GetRolesAsync(CancellationToken ct = default)
        {
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .ToListAsync(ct);

            return roles;
        }

        public async Task<int> GetRolesCountAsync(CancellationToken ct = default)
        {
            var count = await _roleManager.Roles
                .AsNoTracking()
                .CountAsync(ct);

            return count;
        }

        public async Task<int> GetUsersCountAsync(CancellationToken ct = default)
        {
            string cacheKey = "usersCount";

            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return cachedCount;
            }

            var count = await _userManager.Users.CountAsync();

            _cache.Set(cacheKey, count,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

            return count;
        }

        public async Task<User> GetOneUserForServiceAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new UserNotFoundException(userId);

            return user;
        }

        public async Task<UserDto> GetOneUserAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId);
            var userDto = _mapper.Map<UserDto>(user);

            return userDto;
        }

        public async Task<OperationResult<UserDto>> ResetPasswordAsync(ResetPasswordDto model)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var user = await GetOneUserForServiceAsync(userId);

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

            var user = await GetOneUserForServiceAsync(userId);

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

            await _notificationService.CreateAsync(new NotificationDtoForCreation
            {
                NotificationType = NotificationType.Settings,
                Title = "Şifreniz güncellendi",
                Description = $"Şifreniz {DateTime.Now.ToString()} tarihinde güncellendi.",
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

        public async Task<OperationResult<UserDto>> CreateUserAsync(UserDtoForCreation userDto)
        {
            try
            {
                var user = _mapper.Map<User>(userDto);

                user.ValidateForCreation();

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                var result = await _userManager.CreateAsync(user, userDto.Password!);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("User creation failed. Errors: {Errors}", errors);
                    return OperationResult<UserDto>.Failure($"Kullanıcı oluşturulamadı: {errors}", ResultType.ValidationError);
                }

                if (userDto.RolesList?.Count > 0)
                {
                    var roleResult = await _userManager.AddToRolesAsync(user, userDto.RolesList!);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("Role assignment failed for user {UserId}. Errors: {Errors}", user.Id, errors);
                        return OperationResult<UserDto>.Failure($"Roller atanamadı: {errors}", ResultType.ValidationError);
                    }
                }

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "User",
                    entityId: user.Id,
                    newValues: new
                    {
                        user.Email,
                        user.UserName,
                        user.PhoneNumber,
                        user.FirstName,
                        user.LastName,
                        userDto.RolesList
                    }
                );

                await _activityService.LogAsync(
                    "Yeni Üye", 
                    $"{user.FirstName} {user.LastName} siteye kayıt oldu.", 
                    "fa-user-plus", 
                    "text-green-500 bg-green-100", 
                    $"/admin/users/edit/{user.Id}"
                );

                _logger.LogInformation("User created successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                return OperationResult<UserDto>.Success("Kullanıcı başarıyla oluşturuldu.");
            }
            catch (UserValidationException ex)
            {
                _logger.LogWarning(ex, "User validation failed. Email: {Email}", userDto.Email);
                return OperationResult<UserDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserDto>> UpdateUserAsync(UserDtoForUpdate userDtoForUpdate)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                var user = await GetOneUserForServiceAsync(userId);
                var oldValues = new
                {
                    user.Email,
                    user.PhoneNumber,
                    user.FirstName,
                    user.LastName,
                };

                _mapper.Map(userDtoForUpdate, user);

                user.ValidateForUpdate();

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("User update failed for {UserId}. Errors: {Errors}", userId, errors);
                    return OperationResult<UserDto>.Failure($"Kullanıcı güncellenemedi: {errors}", ResultType.ValidationError);
                }

                await _notificationService.CreateAsync(new NotificationDtoForCreation
                {
                    NotificationType = NotificationType.Settings,
                    Title = "Profil bilgileriniz güncellendi",
                    Description = $"Profil bilgileriniz {DateTime.Now.ToString()} tarihinde güncellendi.",
                    UserId = user.Id
                });

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "User",
                    entityId: user.Id,
                    oldValues: oldValues,
                    newValues: new
                    {
                        user.Email,
                        user.PhoneNumber,
                        user.FirstName,
                        user.LastName,
                    }
                );

                _logger.LogInformation("User updated successfully. UserId: {UserId}", userId);
                return OperationResult<UserDto>.Success("Kullanıcı başarıyla güncellendi.");
            }
            catch (UserValidationException ex)
            {
                _logger.LogWarning(ex, "User validation failed during update. Email: {Email}", userDtoForUpdate.Email);
                return OperationResult<UserDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserDto>> UpdateUserForAdminAsync(UserDtoForUpdateAdmin userDtoForUpdate)
        {
            try
            {
                var user = await GetOneUserForServiceAsync(userDtoForUpdate.Id);
                var oldUser = user;
                var roles = await _userManager.GetRolesAsync(user);

                _mapper.Map(userDtoForUpdate, user);

                user.ValidateForUpdate();

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Admin user update failed for {UserId}. Errors: {Errors}", user.Id, errors);
                    return OperationResult<UserDto>.Failure($"Kullanıcı güncellenemedi: {errors}", ResultType.ValidationError);
                }

                if (userDtoForUpdate.RolesList?.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, roles);
                    var roleResult = await _userManager.AddToRolesAsync(user, userDtoForUpdate.RolesList!);

                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("Role update failed for user {UserId}. Errors: {Errors}", user.Id, errors);
                        return OperationResult<UserDto>.Failure($"Roller güncellenemedi: {errors}", ResultType.ValidationError);
                    }
                }

                await _notificationService.CreateAsync(new NotificationDtoForCreation
                {
                    NotificationType = NotificationType.Settings,
                    Title = "Profil bilgileriniz güncellendi",
                    Description = $"Profil bilgileriniz {DateTime.Now.ToString()} tarihinde güncellendi.",
                    UserId = user.Id
                });

                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "User",
                    entityId: user.Id,
                    oldValues: new
                    {
                        oldUser.Email,
                        oldUser.PhoneNumber,
                        oldUser.FirstName,
                        oldUser.LastName,
                        roles
                    },
                    newValues: new
                    {
                        user.Email,
                        user.PhoneNumber,
                        user.FirstName,
                        user.LastName,
                        userDtoForUpdate.RolesList
                    }
                );

                _logger.LogInformation("User updated by admin successfully. UserId: {UserId}, AdminId: {AdminId}", user.Id, userId);
                return OperationResult<UserDto>.Success("Kullanıcı başarıyla güncellendi.");
            }
            catch (UserValidationException ex)
            {
                _logger.LogWarning(ex, "User validation failed during admin update. UserId: {UserId}", userDtoForUpdate.Id);
                return OperationResult<UserDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserDto>> DeleteUserAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId);

            var userIdForLog = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            user.SoftDelete(userIdForLog);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User deletion failed for {UserId}. Errors: {Errors}", userId, errors);
                return OperationResult<UserDto>.Failure($"Kullanıcı silinemedi: {errors}", ResultType.ValidationError);
            }

            await _auditLogService.LogAsync(
                userId: userIdForLog,
                userName: userName,
                action: "Delete",
                entityName: "User",
                entityId: user.Id
            );

            _logger.LogInformation("User soft deleted. UserId: {UserId}, DeletedBy: {DeletedBy}", userId, userIdForLog);
            return OperationResult<UserDto>.Success("Kullanıcı başarıyla silindi.");
        }

        public async Task<FavouriteResultDto> GetOneUsersFavouritesAsync(string userId)
        {
            var favourites = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FavouriteProductVariantsId)
                .FirstOrDefaultAsync();

            var favouritesDto = new FavouriteResultDto
            {
                FavouriteProductVariantsId = favourites?.ToList() ?? []
            };

            return favouritesDto;
        }

        public async Task<OperationResult<FavouriteResultDto>> AddToFavouritesAsync(int productVariantId)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var user = await GetOneUserForServiceAsync(userId);

            if (user.FavouriteProductVariantsId.Contains(productVariantId))
            {
                return OperationResult<FavouriteResultDto>.Failure("Ürün zaten favorilerde.", ResultType.ValidationError);
            }

            user.FavouriteProductVariantsId.Add(productVariantId);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Favourites addition failed for user {UserId}", userId);
                return OperationResult<FavouriteResultDto>.Failure("Favorilere eklenemedi.", ResultType.ValidationError);
            }
            _logger.LogInformation("Product added to favourites successfully for user {UserId}", userId);

            return OperationResult<FavouriteResultDto>.Success(new FavouriteResultDto { FavouriteProductVariantsId = user.FavouriteProductVariantsId?.ToList() ?? [] }, "Ürün favorilere eklendi.");
        }

        public async Task<OperationResult<FavouriteResultDto>> RemoveFromFavouritesAsync(int productVariantId)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var user = await GetOneUserForServiceAsync(userId);

            if (!user.FavouriteProductVariantsId!.Contains(productVariantId))
            {
                return OperationResult<FavouriteResultDto>.Failure("Ürün favorilerde bulunamadı.", ResultType.ValidationError);
            }

            user.FavouriteProductVariantsId.Remove(productVariantId);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Favourites removal failed for user {UserId}", userId);
                return OperationResult<FavouriteResultDto>.Failure("Favorilerden kaldırılamadı.", ResultType.ValidationError);
            }
            _logger.LogInformation("Product removed from favourites successfully for user {UserId}", userId);

            return OperationResult<FavouriteResultDto>.Success(new FavouriteResultDto { FavouriteProductVariantsId = user.FavouriteProductVariantsId?.ToList() ?? [] }, "Ürün favorilerden kaldırıldı.");
        }

        public async Task<OperationResult<FavouriteResultDto>> UpdateUserFavouritesAsync(List<int> favouriteProductVariantIds)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            var user = await GetOneUserForServiceAsync(userId);
            user.FavouriteProductVariantsId = favouriteProductVariantIds;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Favourites update failed for user {UserId}", userId);
                return OperationResult<FavouriteResultDto>.Failure("Favoriler güncellenemedi.", ResultType.ValidationError);
            }
            _logger.LogInformation("Favourites updated successfully for user {UserId}", userId);
            return OperationResult<FavouriteResultDto>.Success(new FavouriteResultDto { FavouriteProductVariantsId = user.FavouriteProductVariantsId?.ToList() ?? [] }, "Favoriler başarıyla güncellendi.");
        }
    }
}