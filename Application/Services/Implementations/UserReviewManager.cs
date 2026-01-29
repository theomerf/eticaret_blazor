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
        private readonly ILogger<UserReviewManager> _logger;

        public UserReviewManager(
            IRepositoryManager manager,
            IAuthService authService,
            IAuditLogService auditLogService,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IProductService productService,
            ISecurityLogService securityLogService,
            ILogger<UserReviewManager> logger)
        {
            _manager = manager;
            _authService = authService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _securityLogService = securityLogService;
            _logger = logger;
        }

        public async Task<IEnumerable<UserReviewDto>> GetAllUserReviewsAsync()
        {
            var reviews = await _manager.UserReview.GetAllUserReviewsAsync(false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<int> GetCountAsync() => await _manager.UserReview.CountAsync(false);

        public async Task<UserReview> GetOneUserReviewForServiceAsync(int id, bool trackChanges)
        {
            var userReview = await _manager.UserReview.GetOneUserReviewAsync(id, trackChanges);
            if (userReview == null)
            {
                throw new UserReviewNotFoundException(id);
            }

            return userReview;
        }

        public async Task<UserReviewDto> GetOneUserReviewAsync(int id)
        {
            var userReview = await GetOneUserReviewForServiceAsync(id, false);
            var userReviewDto = _mapper.Map<UserReviewDto>(userReview);

            return userReviewDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneProductAsync(int id)
        {
            var reviews = await _manager.UserReview.GetAllUserReviewsOfOneProductAsync(id, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneProductAdminAsync(int id)
        {
            var reviews = await _manager.UserReview.GetAllUserReviewsOfOneProductAdminAsync(id, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneUserAsync(string id)
        {
            var reviews = await _manager.UserReview.GetAllUserReviewsOfOneUserAsync(id, false);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(reviews);

            return reviewsDto;
        }

        public async Task<OperationResult<UserReviewDto>> CreateUserReviewAsync(UserReviewDtoForCreation userReviewDto)
        {
            try
            {
                var userReview = _mapper.Map<UserReview>(userReviewDto);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                var user = await _authService.GetOneUserAsync(userId);

                userReview.UserId = userId;
                userReview.ReviewerName = user.FirstName;

                userReview.ValidateForCreation();

                _manager.UserReview.CreateUserReview(userReview);
                await _manager.SaveAsync();

                _logger.LogInformation(
                    "User review created. ReviewId: {ReviewId}, ProductId: {ProductId}, UserId: {UserId}",
                    userReview.UserReviewId, userReview.ProductId, userId);

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla oluşturuldu.");
            }
            catch (UserReviewValidationException ex)
            {
                _logger.LogWarning(ex, "User review validation failed. ProductId: {ProductId}", userReviewDto.ProductId);
                return OperationResult<UserReviewDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserReviewDto>> ApproveUserReviewAsync(int id)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(id, true);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var product = await _productService.GetOneProductAsync(userReview.ProductId);

            userReview.Approve();

            product.AverageRating = ((product.AverageRating * product.ReviewCount) + userReview.Rating) / (product.ReviewCount + 1);
            product.ReviewCount += 1;

            var productEntity = _mapper.Map<Product>(product);
            _manager.Product.Update(productEntity);

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

        public async Task<OperationResult<UserReviewDto>> UpdateUserReviewFeaturedStatusAsync(int id)
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

        public async Task<OperationResult<UserReviewDto>> UpdateUserReviewAsync(UserReviewDtoForUpdate userReviewDto)
        {
            try
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewDto.UserReviewId, true);
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                if (userReview.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
                }

                var product = await _productService.GetOneProductAsync(userReview.ProductId);

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
                _manager.Product.Update(productEntity);

                await _manager.SaveAsync();

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

        public async Task<OperationResult<UserReviewDto>> DeleteUserReviewAsync(int id)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(id, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (userReview.UserId != userId)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }

            var product = await _productService.GetOneProductAsync(userReview.ProductId);

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
            _manager.Product.Update(productEntity);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "User review soft deleted. ReviewId: {ReviewId}, UserId: {UserId}",
                id, userId);

            return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
        }

        public async Task<OperationResult<UserReviewDto>> DeleteUserReviewForAdminAsync(int id)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(id, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var product = await _productService.GetOneProductAsync(userReview.ProductId);

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
            _manager.Product.Update(productEntity);

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: "Delete",
                entityName: "UserReview",
                entityId: id.ToString()
            );

            _logger.LogInformation(
                "User review deleted by admin. ReviewId: {ReviewId}, AdminId: {AdminId}",
                id, userId);

            return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
        }
    }
}
