using AutoMapper;
using Application.Services.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Application.Common.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class UserReviewManager : IUserReviewService
    {
        private readonly IRepositoryManager _manager;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;
        private readonly ISecurityLogService _securityLogService;
        private readonly IActivityService _activityService;
        private readonly ILogger<UserReviewManager> _logger;
        private readonly IFileService _fileService;
        private readonly ICacheService _cache;

        public UserReviewManager(
            IRepositoryManager manager,
            IAuthService authService,
            IAuditLogService auditLogService,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IProductService productService,
            ISecurityLogService securityLogService,
            IActivityService activityService,
            ILogger<UserReviewManager> logger,
            IFileService fileService,
            ICacheService cache)
        {
            _manager = manager;
            _authService = authService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _securityLogService = securityLogService;
            _activityService = activityService;
            _logger = logger;
            _fileService = fileService;
            _cache = cache;
        }

        public async Task<IEnumerable<UserReviewDto>> GetAllAsync()
        {
            var reviews = await _manager.UserReview.GetAllAsync(false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<int> CountAsync(CancellationToken ct = default) 
        {
            return await _cache.GetOrCreateAsync("userReviews:count",
                async () =>
                {
                    return await _manager.UserReview.CountAsync(false, ct);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        } 

        public async Task<UserReview> GetOneUserReviewForServiceAsync(int id, bool trackChanges)
        {
            var userReview = await _manager.UserReview.GetByIdAsync(id, trackChanges);
            if (userReview == null)
            {
                throw new UserReviewNotFoundException(id);
            }

            return userReview;
        }

        public async Task<UserReviewDto> GetByIdAsync(int userReviewId)
        {
            var userReview = await GetOneUserReviewForServiceAsync(userReviewId, false);
            var userReviewDto = _mapper.Map<UserReviewDto>(userReview);

            return userReviewDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetByProductIdAsync(int productId)
        {
            var reviews = await _manager.UserReview.GetByProductIdAsync(productId, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetByProductIdAdminAsync(int productId)
        {
            var reviews = await _manager.UserReview.GetByProductIdAdminAsync(productId, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetByUserIdAsync(string userId)
        {
            var reviews = await _manager.UserReview.GetByUserIdAsync(userId, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<OperationResult<UserReviewDto>> CreateAsync(UserReviewDtoForCreation userReviewDto)
        {
            try
            {
                var userReview = _mapper.Map<UserReview>(userReviewDto);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var user = await _authService.GetOneUserAsync(userId);

                userReview.UserId = userId;
                userReview.ReviewerName = user.FirstName;

                userReview.ValidateForCreation();

                _manager.UserReview.Create(userReview);
                await _manager.SaveAsync();

                await _activityService.LogAsync(
                    "Yeni Yorum",
                    $"{userReview.ReviewerName}, bir ürüne yorum yaptı.",
                    "fa-star",
                    "text-yellow-500 bg-yellow-100"
                );

                _logger.LogInformation(
                    "User review created. ReviewId: {ReviewId}, ProductId: {ProductId}, UserId: {UserId}",
                    userReview.UserReviewId, userReview.ProductId, userId);

                await _cache.RemoveByPrefixAsync("userReviews:");
                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla oluşturuldu.");
            }
            catch (UserReviewValidationException ex)
            {
                _logger.LogWarning(ex, "User review validation failed. ProductId: {ProductId}", userReviewDto.ProductId);
                return OperationResult<UserReviewDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserReviewDto>> ApproveAsync(int id)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(id, true);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var product = await _productService.GetByIdAsync(userReview.ProductId);

            userReview.Approve();

            product.AverageRating = ((product.AverageRating * product.ReviewCount) + userReview.Rating) / (product.ReviewCount + 1);
            product.ReviewCount += 1;

            var productEntity = _mapper.Map<Product>(product);
            _manager.Product.UpdateEntity(productEntity);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Approve",
                entityName: "UserReview",
                entityId: id.ToString()
            );

            _logger.LogInformation(
                "User review approved. ReviewId: {ReviewId}, ApprovedBy: {UserId}",
                id, userId);

            return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla onaylandı.");
        }

        public async Task<OperationResult<UserReviewDto>> UpdateFeaturedStatusAsync(int id)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(id, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            userReview.ToggleFeatured();

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: userReview.IsFeatured ? "Feature" : "Unfeature",
                entityName: "UserReview",
                entityId: id.ToString()
            );

            _logger.LogInformation(
                "User review featured status updated. ReviewId: {ReviewId}, IsFeatured: {IsFeatured}",
                id, userReview.IsFeatured);

            return OperationResult<UserReviewDto>.Success(
                userReview.IsFeatured ? "Değerlendirme başarıyla öne çıkarıldı." : "Değerlendirmenin öne çıkarılması başarıyla iptal edildi.");
        }

        public async Task<OperationResult<UserReviewDto>> UpdateAsync(UserReviewDtoForUpdate userReviewDto)
        {
            try
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewDto.UserReviewId, true);
                var oldImagePath = userReview.ReviewPictureUrl;
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                if (userReview.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                var isImageUpdated =
                    !string.IsNullOrWhiteSpace(userReviewDto.ReviewPictureUrl) &&
                    userReviewDto.ReviewPictureUrl != userReview.ReviewPictureUrl;

                var product = await _productService.GetByIdAsync(userReview.ProductId);

                var oldRatingToRemove = userReview.UpdateReview();

                _mapper.Map(userReviewDto, userReview);

                userReview.ValidateForUpdate();

                if (oldRatingToRemove > 0 && product.ReviewCount > 0)
                {
                    if (product.ReviewCount > 1)
                    {
                        product.AverageRating = ((product.AverageRating * product.ReviewCount) - oldRatingToRemove) / (product.ReviewCount - 1);
                        product.ReviewCount -= 1;
                    }
                    else
                    {
                        product.AverageRating = 0;
                        product.ReviewCount = 0;
                    }
                }

                var productEntity = _mapper.Map<Product>(product);
                _manager.Product.UpdateEntity(productEntity);

                await _manager.SaveAsync();

                if (isImageUpdated && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    _fileService.Delete(oldImagePath);
                }

                _logger.LogInformation(
                    "User review updated. ReviewId: {ReviewId}, UserId: {UserId}, OldRating: {OldRating}, NewRating: {NewRating}, RequiresReapproval: true",
                    userReviewDto.UserReviewId, userId, oldRatingToRemove, userReview.Rating);

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla güncellendi. Yeniden onay bekliyor.");
            }
            catch (UserReviewValidationException ex)
            {
                _logger.LogWarning(ex, "User review validation failed during update. ReviewId: {ReviewId}", userReviewDto.UserReviewId);
                return OperationResult<UserReviewDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserReviewDto>> DeleteAsync(int userReviewId)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var imagePath = userReview.ReviewPictureUrl;

            if (userReview.UserId != userId)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }

            var product = await _productService.GetByIdAsync(userReview.ProductId);

            userReview.SoftDelete(userId);

            if (product.ReviewCount > 1)
            {
                product.AverageRating = ((product.AverageRating * product.ReviewCount) - userReview.Rating) / (product.ReviewCount - 1);
                product.ReviewCount -= 1;
            }
            else
            {
                product.AverageRating = 0;
                product.ReviewCount = 0;
            }

            var productEntity = _mapper.Map<Product>(product);
            _manager.Product.UpdateEntity(productEntity);

            await _manager.SaveAsync();

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                _fileService.Delete(imagePath);
            }

            _logger.LogInformation(
                "User review soft deleted. ReviewId: {ReviewId}, UserId: {UserId}",
                userReviewId, userId);

            await _cache.RemoveByPrefixAsync("userReviews:");
            return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
        }

        public async Task<OperationResult<UserReviewDto>> DeleteAdminAsync(int userReviewId)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
            var imagePath = userReview.ReviewPictureUrl;

            var product = await _productService.GetByIdAsync(userReview.ProductId);

            userReview.SoftDelete(userId);

            if (product.ReviewCount > 1)
            {
                product.AverageRating = ((product.AverageRating * product.ReviewCount) - userReview.Rating) / (product.ReviewCount - 1);
                product.ReviewCount -= 1;
            }
            else
            {
                product.AverageRating = 0;
                product.ReviewCount = 0;
            }

            var productEntity = _mapper.Map<Product>(product);
            _manager.Product.UpdateEntity(productEntity);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "UserReview",
                entityId: userReviewId.ToString()
            );

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                _fileService.Delete(imagePath);
            }

            _logger.LogInformation(
                "User review deleted by admin. ReviewId: {ReviewId}, AdminId: {AdminId}",
                userReviewId, userId);

            await _cache.RemoveByPrefixAsync("userReviews:");
            return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
        }
    }
}
