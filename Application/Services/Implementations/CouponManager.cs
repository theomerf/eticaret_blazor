using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class CouponManager : ICouponService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ILogger<CouponManager> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecurityLogService _securityLogService;
        private readonly IActivityService _activityService;
        private readonly ICacheService _cache;

        public CouponManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<CouponManager> logger,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogService auditLogService,
            ISecurityLogService securityLogService,
            IActivityService activityService,
            ICacheService cache)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
            _securityLogService = securityLogService;
            _activityService = activityService;
            _cache = cache;
        }

        private string GetCurrentUserId() =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string GetCurrentUserName() =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        public async Task<(IEnumerable<CouponDto> coupons, int count, int activeCount)> GetAllAdminAsync(CouponRequestParametersAdmin p, CancellationToken ct = default)
        {
            var result = await _manager.Coupon.GetAllAdminAsync(p, false, ct);
            var couponsDto = _mapper.Map<IEnumerable<CouponDto>>(result.coupons);

            return (couponsDto, result.count, result.activeCount);
        }

        public async Task<int> CountOfActiveAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "coupons:activeCount",
                async token =>
                {
                    return await _manager.Coupon.CountOfActiveAsync(token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<OperationResult<CouponDto>> GetByIdAsync(int couponId)
        {
            var coupon = await _manager.Coupon.GetByIdAsync(couponId, false);
            if (coupon == null)
            {
                return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
            }

            var couponDto = _mapper.Map<CouponDto>(coupon);
            return OperationResult<CouponDto>.Success(couponDto);
        }

        public async Task<OperationResult<CouponDto>> GetByCodeAsync(string code)
        {
            var coupon = await _manager.Coupon.GetByCodeAsync(code, false);
            if (coupon == null)
            {
                return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
            }

            var couponDto = _mapper.Map<CouponDto>(coupon);
            return OperationResult<CouponDto>.Success(couponDto);
        }

        public async Task<OperationResult<int>> CreateAsync(CouponDtoForCreation couponDto)
        {
            try
            {
                // Validate code uniqueness
                var isUnique = await _manager.Coupon.IsCouponCodeUniqueAsync(couponDto.Code);
                if (!isUnique)
                {
                    return OperationResult<int>.Failure("Bu kupon kodu zaten kullanılıyor.", ResultType.ValidationError);
                }

                var coupon = _mapper.Map<Coupon>(couponDto);
                coupon.Code = coupon.Code.ToUpper(); // Normalize code

                // Domain validation
                coupon.ValidateForCreation();

                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                coupon.CreatedByUserId = userId;
                coupon.UpdatedByUserId = userId;

                _manager.Coupon.Create(coupon);
                await _manager.SaveAsync();

                // Audit log
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Create",
                    entityName: "Coupon",
                    entityId: coupon.CouponId.ToString(),
                    newValues: new
                    {
                        coupon.Code,
                        coupon.Type,
                        coupon.Value,
                        coupon.Scope,
                        coupon.StartsAt,
                        coupon.EndsAt,
                        coupon.UsageLimit
                    }
                );

                await _activityService.LogAsync(
                    "Yeni Kupon",
                    $"{coupon.Code} kuponu oluşturuldu.",
                    "fa-ticket-alt",
                    "text-purple-500 bg-purple-100",
                    $"/admin/coupons/edit/{coupon.CouponId}"
                );

                _logger.LogInformation(
                    "Coupon created successfully. CouponId: {CouponId}, Code: {Code}, User: {UserId}",
                    coupon.CouponId, coupon.Code, userId);

                await _cache.RemoveByPrefixAsync("coupons:");
                return OperationResult<int>.Success(coupon.CouponId, "Kupon başarıyla oluşturuldu.");
            }
            catch (CouponValidationException ex)
            {
                _logger.LogWarning(ex, "Coupon validation failed. Code: {Code}", couponDto.Code);
                return OperationResult<int>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CouponDto>> UpdateAsync(CouponDtoForUpdate couponDto)
        {
            try
            {
                _manager.ClearTracker();
                var coupon = await _manager.Coupon.GetByIdAsync(couponDto.CouponId, true);
                if (coupon == null)
                {
                    return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
                }

                // Validate code uniqueness if changed
                if (coupon.Code != couponDto.Code.ToUpper())
                {
                    var isUnique = await _manager.Coupon.IsCouponCodeUniqueAsync(couponDto.Code, couponDto.CouponId);
                    if (!isUnique)
                    {
                        return OperationResult<CouponDto>.Failure("Bu kupon kodu zaten kullanılıyor.", ResultType.ValidationError);
                    }
                }

                if (couponDto.UsageLimit < coupon.UsedCount)
                {
                    return OperationResult<CouponDto>.Failure(
                        $"Kullanım limiti mevcut kullanım sayısından düşük olamaz. (Mevcut kullanım: {coupon.UsedCount})",
                        ResultType.ValidationError);
                }

                var oldValues = new
                {
                    coupon.Code,
                    coupon.Type,
                    coupon.Value,
                    coupon.Scope,
                    coupon.StartsAt,
                    coupon.EndsAt,
                    coupon.UsageLimit,
                    coupon.IsActive
                };

                _mapper.Map(couponDto, coupon);
                coupon.Code = coupon.Code.ToUpper();

                // Domain validation
                coupon.ValidateForCreation();

                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                coupon.UpdatedAt = DateTime.UtcNow;
                coupon.UpdatedByUserId = userId;

                await _manager.SaveAsync();

                // Audit log
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Update",
                    entityName: "Coupon",
                    entityId: coupon.CouponId.ToString(),
                    oldValues: oldValues,
                    newValues: new
                    {
                        coupon.Code,
                        coupon.Type,
                        coupon.Value,
                        coupon.Scope,
                        coupon.StartsAt,
                        coupon.EndsAt,
                        coupon.UsageLimit,
                        coupon.IsActive
                    }
                );

                _logger.LogInformation(
                    "Coupon updated successfully. CouponId: {CouponId}, User: {UserId}",
                    coupon.CouponId, userId);

                var updatedCouponDto = _mapper.Map<CouponDto>(coupon);
                await _cache.RemoveByPrefixAsync("coupons:");
                return OperationResult<CouponDto>.Success(updatedCouponDto, "Kupon başarıyla güncellendi.");
            }
            catch (CouponValidationException ex)
            {
                _logger.LogWarning(ex, "Coupon validation failed during update. CouponId: {CouponId}", couponDto.CouponId);
                return OperationResult<CouponDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<CouponDto>> DeleteAsync(int couponId)
        {
            var coupon = await _manager.Coupon.GetByIdAsync(couponId, true);
            if (coupon == null)
            {
                return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            coupon.SoftDelete(userId);
            await _manager.SaveAsync();

            // Audit log
            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "Coupon",
                entityId: couponId.ToString()
            );

            _logger.LogInformation(
                "Coupon soft deleted. CouponId: {CouponId}, User: {UserId}",
                couponId, userId);

            await _cache.RemoveByPrefixAsync("coupons:");
            return OperationResult<CouponDto>.Success("Kupon başarıyla silindi.");
        }

        public async Task<OperationResult<CouponDto>> ActivateAsync(int couponId)
        {
            _manager.ClearTracker();
            var coupon = await _manager.Coupon.GetByIdAsync(couponId, true);
            if (coupon == null)
            {
                return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            coupon.Activate();
            coupon.UpdatedAt = DateTime.UtcNow;
            coupon.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            // Audit log
            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Activate",
                entityName: "Coupon",
                entityId: couponId.ToString(),
                newValues: new { IsActive = true }
            );

            _logger.LogInformation(
                "Coupon activated. CouponId: {CouponId}, User: {UserId}",
                couponId, userId);

            var couponDto = _mapper.Map<CouponDto>(coupon);
            await _cache.RemoveByPrefixAsync("coupons:");
            return OperationResult<CouponDto>.Success(couponDto, "Kupon aktif edildi.");
        }

        public async Task<OperationResult<CouponDto>> DeactivateAsync(int couponId)
        {
            _manager.ClearTracker();
            var coupon = await _manager.Coupon.GetByIdAsync(couponId, true);
            if (coupon == null)
            {
                return OperationResult<CouponDto>.Failure("Kupon bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            coupon.Deactivate();
            coupon.UpdatedAt = DateTime.UtcNow;
            coupon.UpdatedByUserId = userId;

            await _manager.SaveAsync();

            // Audit log
            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Deactivate",
                entityName: "Coupon",
                entityId: couponId.ToString(),
                newValues: new { IsActive = false }
            );

            _logger.LogInformation(
                "Coupon deactivated. CouponId: {CouponId}, User: {UserId}",
                couponId, userId);

            var couponDto = _mapper.Map<CouponDto>(coupon);
            await _cache.RemoveByPrefixAsync("coupons:");
            return OperationResult<CouponDto>.Success(couponDto, "Kupon deaktif edildi.");
        }

        public async Task<OperationResult<decimal>> ValidateAndCalculateDiscountAsync(string code, decimal orderAmount, string userId)
        {
            var coupon = await _manager.Coupon.GetByCodeAsync(code, false);
            if (coupon == null)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: $"/api/coupons/validate/{code}"
                );
                return OperationResult<decimal>.Failure("Geçersiz kupon kodu.", ResultType.ValidationError);
            }

            if (!coupon.IsValid())
            {
                return OperationResult<decimal>.Failure("Kupon geçerli değil veya süresi dolmuş.", ResultType.ValidationError);
            }

            if (coupon.IsSingleUsePerUser)
            {
                var usageCount = await _manager.Coupon.GetUserCouponUsageCountAsync(coupon.CouponId, userId);
                if (usageCount > 0)
                {
                    return OperationResult<decimal>.Failure("Bu kuponu daha once kullandiniz.", ResultType.ValidationError);
                }
            }

            if (!coupon.CanBeUsedBy(userId, orderAmount))
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: $"/api/coupons/validate/{code}"
                );
                return OperationResult<decimal>.Failure("Bu kuponu kullanma yetkiniz yok veya minimum sipariş tutarı karşılanmadı.", ResultType.ValidationError);
            }

            var discount = coupon.CalculateDiscount(orderAmount);
            return OperationResult<decimal>.Success(discount);
        }

        public async Task<OperationResult<Coupon>> ValidateForOrderAsync(string code, decimal orderAmount, string userId)
        {
            var coupon = await _manager.Coupon.GetByCodeAsync(code, false);
            if (coupon == null)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: $"/api/coupons/validate/{code}"
                );
                return OperationResult<Coupon>.Failure("Geçersiz kupon kodu.", ResultType.ValidationError);
            }

            if (!coupon.IsValid())
            {
                return OperationResult<Coupon>.Failure("Kupon geçerli değil veya süresi dolmuş.", ResultType.ValidationError);
            }

            if (coupon.IsSingleUsePerUser)
            {
                var usageCount = await _manager.Coupon.GetUserCouponUsageCountAsync(coupon.CouponId, userId);
                if (usageCount > 0)
                {
                    return OperationResult<Coupon>.Failure("Bu kuponu daha once kullandiniz.", ResultType.ValidationError);
                }
            }

            if (!coupon.CanBeUsedBy(userId, orderAmount))
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: $"/api/coupons/validate/{code}"
                );
                return OperationResult<Coupon>.Failure("Bu kuponu kullanma yetkiniz yok veya minimum sipariş tutarı karşılanmadı.", ResultType.ValidationError);
            }

            return OperationResult<Coupon>.Success(coupon);
        }

        public async Task<IEnumerable<CouponUsage>> GetCouponUsagesByCouponIdAsync(int couponId)
        {
            var usages = await _manager.CouponUsage.GetAllAsync(couponId, false);
            return usages;
        }

        public async Task<IEnumerable<CouponUsage>> GetCouponUsagesByUserIdAsync(string userId)
        {
            var usages = await _manager.CouponUsage.GetByUserIdAsync(userId, false);
            return usages;
        }
    }
}
