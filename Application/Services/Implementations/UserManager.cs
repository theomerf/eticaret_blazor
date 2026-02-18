using Application.Common.Exceptions;
using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class UserManager : IUserService
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly IActivityService _activityService;
        private readonly ILogger<UserManager> _logger;
        private readonly IRepositoryManager _manager;

        public UserManager(
            RoleManager<Role> roleManager,
            UserManager<User> userManager,
            IMapper mapper,
            ICacheService cache,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            INotificationQueueService notificationQueueService,
            IActivityService activityService,
            ILogger<UserManager> logger,
            IRepositoryManager manager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _notificationQueueService = notificationQueueService;
            _activityService = activityService;
            _logger = logger;
            _manager = manager;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDto = _mapper.Map<IEnumerable<UserDto>>(users);

            return userDto;
        }

        public async Task<(IEnumerable<UserDto> users, int count, int activeCount)> GetAllUsersAdminAsync(UserRequestParametersAdmin p, CancellationToken ct = default)
        {
            var result = await _manager.User.GetAllAdminAsync(p, false, ct);
            var usersDto = _mapper.Map<IEnumerable<UserDto>>(result.users);

            return (usersDto, result.count, result.activeCount);
        }

        public async Task<int> GetUsersCountAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "users:count",
                async token =>
                {
                    return await _userManager.Users.CountAsync(token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<IEnumerable<Role>> GetRolesAsync(CancellationToken ct = default)
        {
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .ToListAsync(ct);

            return roles;
        }

        public async Task<int> GetRolesCountAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "users:rolesCount",
                async token =>
                {
                    return await _roleManager.Roles
                        .AsNoTracking()
                        .CountAsync(token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<User> GetOneUserForServiceAsync(string userId, bool trackChanges, CancellationToken ct = default)
        {
            var user = await _manager.User.GetByIdAsync(userId, trackChanges, ct);
            if (user == null)
                throw new UserNotFoundException(userId);

            return user;
        }

        public async Task<UserDto> GetOneUserAsync(string userId, CancellationToken ct = default)
        {
            var user = await GetOneUserForServiceAsync(userId, false, ct);
            var userDto = _mapper.Map<UserDto>(user);

            return userDto;
        }

        public async Task<OperationResult<UserDto>> CreateUserAsync(UserDtoForCreation userDto, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
            var user = _mapper.Map<User>(userDto);

            try
            {
                var result = await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    user.ValidateForCreation();

                    var result = await _userManager.CreateAsync(user, userDto.Password!);

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new UserCreationException(OperationResult<UserDto>.Failure($"Kullanıcı oluşturulamadı: {errors}", ResultType.ValidationError));
                    }

                    if (userDto.RolesList?.Count > 0)
                    {
                        var roleResult = await _userManager.AddToRolesAsync(user, userDto.RolesList!);
                        if (!roleResult.Succeeded)
                        {
                            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            throw new UserCreationException(OperationResult<UserDto>.Failure($"Roller atanamadı: {errors}", ResultType.ValidationError));
                        }
                    }

                    return OperationResult<UserDto>.Success("Kullanıcı başarıyla oluşturuldu.");
                }, ct: ct);

                if (result.IsSuccess && !ct.IsCancellationRequested)
                {
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

                    await _cache.RemoveByPrefixAsync("users:");
                    _logger.LogInformation("User created successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                }

                return result;
            }
            catch (UserCreationException ex)
            {
                _logger.LogWarning(ex.Result.Message);
                return ex.Result;
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

                var user = await GetOneUserForServiceAsync(userId, true);
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

                _notificationQueueService.EnqueueCreate(new NotificationDtoForCreation
                {
                    NotificationType = NotificationType.Settings,
                    Title = "Profil bilgileriniz güncellendi",
                    Description = $"Profil bilgileriniz {DateTime.Now} tarihinde güncellendi.",
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

        public async Task<OperationResult<UserDto>> UpdateUserForAdminAsync(UserDtoForUpdateAdmin userDtoForUpdate, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            User? user = null;
            IList<string>? roles = null;
            object? oldValuesSnapshot = null;

            try
            {
                var result = await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    user = await GetOneUserForServiceAsync(userDtoForUpdate.Id, true);
                    roles = await _userManager.GetRolesAsync(user);
                    oldValuesSnapshot = new
                    {
                        user.Email,
                        user.PhoneNumber,
                        user.FirstName,
                        user.LastName,
                        Roles = roles.ToList()
                    };

                    _mapper.Map(userDtoForUpdate, user);

                    user.ValidateForUpdate();

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
                    return OperationResult<UserDto>.Success("Kullanıcı başarıyla güncellendi.");
                }, ct: ct);

                if (result.IsSuccess && !ct.IsCancellationRequested)
                {
                    _notificationQueueService.EnqueueCreate(new NotificationDtoForCreation
                    {
                        NotificationType = NotificationType.Settings,
                        Title = "Profil bilgileriniz güncellendi",
                        Description = $"Profil bilgileriniz {DateTime.Now} tarihinde güncellendi.",
                        UserId = user!.Id
                    });

                    await _auditLogService.LogAsync(
                        userId: userId,
                        userName: userName,
                        action: "Update",
                        entityName: "User",
                        entityId: user.Id,
                        oldValues: oldValuesSnapshot,
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
                }

                return result;

            }
            catch (UserUpdateException ex)
            {
                _logger.LogWarning(ex.Result.Message);
                return ex.Result;
            }
            catch (UserValidationException ex)
            {
                _logger.LogWarning(ex, "User validation failed during admin update. UserId: {UserId}", userDtoForUpdate.Id);
                return OperationResult<UserDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserDto>> DeleteUserAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId, true);

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

            await _cache.RemoveByPrefixAsync("users:");
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
            var user = await GetOneUserForServiceAsync(userId, true);

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
            var user = await GetOneUserForServiceAsync(userId, true);

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

            var user = await GetOneUserForServiceAsync(userId, true);
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

        public async Task<OperationResult<UserDto>> ToggleUserActiveAsync(string userId)
        {
            var user = await GetOneUserForServiceAsync(userId, true);
            var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var oldStatus = user.IsActive;
            user.IsActive = !user.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Toggle active failed for user {UserId}. Errors: {Errors}", userId, errors);
                return OperationResult<UserDto>.Failure($"Kullanıcı durumu güncellenemedi: {errors}", ResultType.ValidationError);
            }

            await _auditLogService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: user.IsActive ? "Activate" : "Deactivate",
                entityName: "User",
                entityId: userId,
                oldValues: new { IsActive = oldStatus },
                newValues: new { IsActive = user.IsActive }
            );

            await _cache.RemoveByPrefixAsync("users:");
            _logger.LogInformation("User {UserId} {Action} by admin {AdminId}", userId, user.IsActive ? "activated" : "deactivated", adminId);

            var dto = _mapper.Map<UserDto>(user);
            var roles = await _userManager.GetRolesAsync(user);
            dto.Roles = roles.ToHashSet();
            return OperationResult<UserDto>.Success(dto, user.IsActive ? "Kullanıcı aktif edildi." : "Kullanıcı pasif edildi.");
        }

        public async Task<OperationResult<UserDto>> ChangeUserRolesAsync(string userId, HashSet<string> newRoles)
        {
            var user = await GetOneUserForServiceAsync(userId, true);
            var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Role removal failed for user {UserId}. Errors: {Errors}", userId, errors);
                return OperationResult<UserDto>.Failure($"Roller kaldırılamadı: {errors}", ResultType.ValidationError);
            }

            if (newRoles.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, newRoles);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Role assignment failed for user {UserId}. Errors: {Errors}", userId, errors);

                    await _userManager.AddToRolesAsync(user, currentRoles);
                    return OperationResult<UserDto>.Failure($"Roller atanamadı: {errors}", ResultType.ValidationError);
                }
            }

            await _auditLogService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: "ChangeRoles",
                entityName: "User",
                entityId: userId,
                oldValues: new { Roles = currentRoles },
                newValues: new { Roles = newRoles }
            );

            await _cache.RemoveByPrefixAsync("users:");
            _logger.LogInformation("Roles changed for user {UserId}. OldRoles: {OldRoles}, NewRoles: {NewRoles}, Admin: {AdminId}",
                userId, string.Join(",", currentRoles), string.Join(",", newRoles), adminId);
            return OperationResult<UserDto>.Success("Kullanıcı rolleri güncellendi.");
        }

        public async Task<OperationResult<UserDto>> UpdateAdminNotesAsync(string userId, string notes)
        {
            var user = await GetOneUserForServiceAsync(userId, true);
            var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var oldNotes = user.AdminNotes;
            user.AdminNotes = notes;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return OperationResult<UserDto>.Failure($"Admin notu güncellenemedi: {errors}", ResultType.ValidationError);
            }

            await _auditLogService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: "UpdateAdminNotes",
                entityName: "User",
                entityId: userId,
                oldValues: new { AdminNotes = oldNotes },
                newValues: new { AdminNotes = notes }
            );

            _logger.LogInformation("Admin notes updated for user {UserId}. Admin: {AdminId}", userId, adminId);
            return OperationResult<UserDto>.Success("Admin notu güncellendi.");
        }

        public async Task<OperationResult<UserDto>> EditUserInfoAsync(UserDtoForAdminEdit dto)
        {
            try
            {
                var user = await GetOneUserForServiceAsync(dto.Id, true);
                var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                var oldValues = new
                {
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber
                };

                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.PhoneNumber = dto.PhoneNumber;

                user.ValidateForUpdate();

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Admin user edit failed for {UserId}. Errors: {Errors}", dto.Id, errors);
                    return OperationResult<UserDto>.Failure($"Kullanıcı bilgileri güncellenemedi: {errors}", ResultType.ValidationError);
                }

                await _auditLogService.LogAsync(
                    userId: adminId,
                    userName: adminName,
                    action: "EditUserInfo",
                    entityName: "User",
                    entityId: dto.Id,
                    oldValues: oldValues,
                    newValues: new
                    {
                        dto.FirstName,
                        dto.LastName,
                        dto.PhoneNumber
                    }
                );

                _logger.LogInformation("User info edited by admin. UserId: {UserId}, AdminId: {AdminId}", dto.Id, adminId);
                return OperationResult<UserDto>.Success("Kullanıcı bilgileri güncellendi.");
            }
            catch (UserValidationException ex)
            {
                _logger.LogWarning(ex, "User validation failed during admin edit. UserId: {UserId}", dto.Id);
                return OperationResult<UserDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<IReadOnlyList<UserEmailLookupDto>> SearchUserEmailLookupAsync(string searchTerm, int take = 10, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
                return [];

            return await _manager.User.SearchEmailLookupAsync(searchTerm, take, trackChanges: false, ct);
        }
    }
}
