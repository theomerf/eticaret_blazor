using Application.Common.Exceptions;
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
    public class UserReviewManager : IUserReviewService
    {
        private readonly IRepositoryManager _manager;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityLogService _securityLogService;
        private readonly IActivityService _activityService;
        private readonly ILogger<UserReviewManager> _logger;
        private readonly IFileService _fileService;
        private readonly ICacheService _cache;

        public UserReviewManager(
            IRepositoryManager manager,
            IUserService userService,
            IAuditLogService auditLogService,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService,
            IActivityService activityService,
            ILogger<UserReviewManager> logger,
            IFileService fileService,
            ICacheService cache)
        {
            _manager = manager;
            _userService = userService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;
            _activityService = activityService;
            _logger = logger;
            _fileService = fileService;
            _cache = cache;
        }

        public async Task<(IEnumerable<UserReviewDto> reviews, int count, int approvedCount)> GetAllAdminAsync(UserReviewRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var result = await _manager.UserReview.GetAllAdminAsync(p, trackChanges, ct);
            var reviewsDto = _mapper.Map<IEnumerable<UserReviewDto>>(result.reviews);

            return (reviewsDto, result.count, result.approvedCount);
        }

        public async Task<int> CountAsync(CancellationToken ct = default) 
        {
            return await _cache.GetOrCreateAsync(
                "userReviews:count",
                async token =>
                {
                    return await _manager.UserReview.CountAsync(false, token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        } 

        public async Task<int> CountApprovedAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "userReviews:approvedCount",
                async token =>
                {
                    return await _manager.UserReview.CountApprovedAsync(token);
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

                var user = await _userService.GetOneUserAsync(userId);

                userReview.UserId = userId;
                userReview.ReviewerName = string.Concat(user.FirstName, " ", user.LastName.FirstOrDefault().ToString(), "***");

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

        public async Task<OperationResult<UserReviewDto>> ApproveAsync(int userReviewId, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var result = await _manager.ExecuteInTransactionAsync(async ct =>
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);

                if (userReview.IsApproved)
                    return OperationResult<UserReviewDto>.Failure("Bu değerlendirme zaten onaylı.", ResultType.ValidationError);

                var product = await _manager.Product.GetByIdAsync(userReview.ProductId, true, true);

                if (product == null)
                    throw new ProductNotFoundException(userReview.ProductId);

                userReview.Approve();

                product.AverageRating = ((product.AverageRating * product.ReviewCount) + userReview.Rating) / (product.ReviewCount + 1);
                product.ReviewCount += 1;

                await _manager.SaveAsync();

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla onaylandı.");
            }, ct: ct);

            if (result.IsSuccess && !ct.IsCancellationRequested)
            {
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Approve",
                    entityName: "UserReview",
                    entityId: userReviewId.ToString()
                );

                _logger.LogInformation(
                    "User review approved. ReviewId: {ReviewId}, ApprovedBy: {UserId}",
                    userReviewId, userId);
            }

            return result;
        }

        public async Task<OperationResult<UserReviewDto>> UnapproveAsync(int userReviewId, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var result = await _manager.ExecuteInTransactionAsync(async ct =>
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);

                if (userReview.IsApproved == false)
                    return OperationResult<UserReviewDto>.Failure("Bu değerlendirme zaten onaylı değil.", ResultType.ValidationError);

                var product = await _manager.Product.GetByIdAsync(userReview.ProductId, true, true);

                if (product == null)
                    throw new ProductNotFoundException(userReview.ProductId);

                userReview.Unapprove();

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

                await _manager.SaveAsync();

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla onaylanmadı.");
            }, ct: ct);

            if (result.IsSuccess && !ct.IsCancellationRequested)
            {
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Unapprove",
                    entityName: "UserReview",
                    entityId: userReviewId.ToString()
                );

                _logger.LogInformation(
                    "User review unapproved. ReviewId: {ReviewId}, UnapprovedBy: {UserId}",
                    userReviewId, userId);
            }

            return result;
        }

        public async Task<OperationResult<UserReviewDto>> UpdateFeaturedStatusAsync(int userReviewId)
        {
            _manager.ClearTracker();
            var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);

            if (!userReview.IsApproved)
            {
                return OperationResult<UserReviewDto>.Failure("Sadece onaylanmış değerlendirmelerin öne çıkarılma durumu değiştirilebilir.", ResultType.ValidationError);
            }

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            userReview.ToggleFeatured();

            await _manager.SaveAsync();

            await _auditLogService.LogAsync(
                userId: userId,
                userName: userName,
                action: userReview.IsFeatured ? "Feature" : "Unfeature",
                entityName: "UserReview",
                entityId: userReviewId.ToString()
            );

            _logger.LogInformation(
                "User review featured status updated. ReviewId: {ReviewId}, IsFeatured: {IsFeatured}",
                userReviewId, userReview.IsFeatured);

            return OperationResult<UserReviewDto>.Success(
                userReview.IsFeatured ? "Değerlendirme başarıyla öne çıkarıldı." : "Değerlendirmenin öne çıkarılması başarıyla iptal edildi.");
        }

        public async Task<OperationResult<UserReviewDto>> UpdateAsync(UserReviewDtoForUpdate userReviewDto, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var requestPath = _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? "";

            string? oldImagePathToDelete = null;
            bool deleteOldImage = false;
            int oldRatingToRemove = 0;
            int newRating = 0;

            try
            {
                var result = await _manager.ExecuteInTransactionAsync(async ct =>
                {
                    _manager.ClearTracker();

                    var userReview = await GetOneUserReviewForServiceAsync(userReviewDto.UserReviewId, true);

                    if (userReview.UserId != userId)
                    {
                        await _securityLogService.LogUnauthorizedAccessAsync(userId: userId, requestPath: requestPath);
                        return OperationResult<UserReviewDto>.Failure("Bunun için yetkiniz yok.", ResultType.Unauthorized);
                    }

                    var oldImagePath = userReview.ReviewPictureUrl;

                    var isImageUpdated =
                        !string.IsNullOrWhiteSpace(userReviewDto.ReviewPictureUrl) &&
                        userReviewDto.ReviewPictureUrl != userReview.ReviewPictureUrl;

                    var product = await _manager.Product.GetByIdAsync(userReview.ProductId, true, true);
                    if (product == null)
                        throw new ProductNotFoundException(userReview.ProductId);

                    oldRatingToRemove = userReview.UpdateReview();

                    _mapper.Map(userReviewDto, userReview);
                    userReview.ValidateForUpdate();

                    newRating = userReview.Rating;

                    if (oldRatingToRemove > 0 && product.ReviewCount > 0)
                    {
                        if (product.ReviewCount > 1)
                        {
                            product.AverageRating =
                                ((product.AverageRating * product.ReviewCount) - oldRatingToRemove) / (product.ReviewCount - 1);

                            product.ReviewCount -= 1;
                        }
                        else
                        {
                            product.AverageRating = 0;
                            product.ReviewCount = 0;
                        }
                    }

                    await _manager.SaveAsync();

                    if (isImageUpdated && !string.IsNullOrWhiteSpace(oldImagePath))
                    {
                        oldImagePathToDelete = oldImagePath;
                        deleteOldImage = true;
                    }

                    return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla güncellendi. Yeniden onay bekliyor.");
                }, ct: ct);

                if (result.IsSuccess && !ct.IsCancellationRequested)
                {
                    if (deleteOldImage && !string.IsNullOrWhiteSpace(oldImagePathToDelete))
                    {
                        _fileService.Delete(oldImagePathToDelete);
                    }

                    _logger.LogInformation(
                        "User review updated. ReviewId: {ReviewId}, UserId: {UserId}, OldRating: {OldRating}, NewRating: {NewRating}, RequiresReapproval: true",
                        userReviewDto.UserReviewId, userId, oldRatingToRemove, newRating);

                    await _cache.RemoveAsync("userReviews:approvedCount", ct);
                }

                return result;
            }
            catch (UserReviewValidationException ex)
            {
                _logger.LogWarning(ex, "User review validation failed during update. ReviewId: {ReviewId}", userReviewDto.UserReviewId);
                return OperationResult<UserReviewDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<UserReviewDto>> DeleteAsync(int userReviewId, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var requestPath = _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? "";

            string? imagePathToDelete = null;

            var result = await _manager.ExecuteInTransactionAsync(async ct =>
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);
                imagePathToDelete = userReview.ReviewPictureUrl;

                if (userReview.UserId != userId)
                {
                    await _securityLogService.LogUnauthorizedAccessAsync(
                        userId: userId,
                        requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                    );
                    return OperationResult<UserReviewDto>.Failure("Bunun için yetkiniz yok.", ResultType.Unauthorized);
                }

                var product = await _manager.Product.GetByIdAsync(userReview.ProductId, true, true);

                if (product == null)
                    throw new ProductNotFoundException(userReview.ProductId);

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

                await _manager.SaveAsync();

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
            }, ct: ct); 

            if (result.IsSuccess && !ct.IsCancellationRequested)
            {
                if (!string.IsNullOrWhiteSpace(imagePathToDelete))
                {
                    _fileService.Delete(imagePathToDelete);
                }

                _logger.LogInformation(
                    "User review soft deleted. ReviewId: {ReviewId}, UserId: {UserId}",
                    userReviewId, userId);

                await _cache.RemoveByPrefixAsync("userReviews:", ct);
            }

            return result;
        }

        public async Task<OperationResult<UserReviewDto>> DeleteAdminAsync(int userReviewId, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            string? imagePathToDelete = null;

            var result = await _manager.ExecuteInTransactionAsync(async ct =>
            {
                _manager.ClearTracker();
                var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);

                imagePathToDelete = userReview.ReviewPictureUrl;

                var product = await _manager.Product.GetByIdAsync(userReview.ProductId, true, true);

                if (product == null)
                    throw new ProductNotFoundException(userReview.ProductId);

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

                await _manager.SaveAsync();

                return OperationResult<UserReviewDto>.Success("Değerlendirme başarıyla silindi.");
            }, ct: ct);

            if (result.IsSuccess && !ct.IsCancellationRequested)
            {
                await _auditLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    action: "Delete",
                    entityName: "UserReview",
                    entityId: userReviewId.ToString()
                );

                if (!string.IsNullOrWhiteSpace(imagePathToDelete))
                {
                    _fileService.Delete(imagePathToDelete);
                }

                _logger.LogInformation(
                    "User review deleted by admin. ReviewId: {ReviewId}, AdminId: {AdminId}",
                    userReviewId, userId);

                await _cache.RemoveByPrefixAsync("userReviews:", ct);
            }

            return result;
        }

        public async Task<OperationResult<(VoteType?, int, int)>> SetVoteAsync(int userReviewId, VoteType? desired, CancellationToken ct = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            if (userId == "System")
                return OperationResult<(VoteType?, int, int)>.Failure("Giriş yapmalısınız.", ResultType.Unauthorized);

            var result = await _manager.ExecuteInTransactionAsync(async ct =>
            {
                _manager.ClearTracker();

                var userReview = await GetOneUserReviewForServiceAsync(userReviewId, true);

                if (userReview.UserId == userId)
                {
                    return OperationResult<(VoteType?, int, int)>.Failure(
                        "Kendi yorumunuzu oylayamazsınız.",
                        ResultType.ValidationError);
                }

                var existingVote = await _manager.UserReview.GetVoteByUserIdAndReviewIdAsync(userId, userReviewId, true);

                int helpfulDelta = 0;
                int notHelpfulDelta = 0;

                if (desired == null)
                {
                    if (existingVote != null)
                    {
                        if (existingVote.VoteType == VoteType.Helpful) helpfulDelta -= 1;
                        else notHelpfulDelta -= 1;

                        _manager.UserReview.DeleteVote(existingVote);
                    }
                }
                else
                {
                    if (existingVote == null)
                    {
                        _manager.UserReview.AddVote(new UserReviewVote
                        {
                            UserId = userId,
                            UserReviewId = userReviewId,
                            VoteType = desired.Value,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                        if (desired.Value == VoteType.Helpful) helpfulDelta += 1;
                        else notHelpfulDelta += 1;
                    }
                    else if (existingVote.VoteType != desired.Value)
                    {
                        if (existingVote.VoteType == VoteType.Helpful) helpfulDelta -= 1;
                        else notHelpfulDelta -= 1;

                        if (desired.Value == VoteType.Helpful) helpfulDelta += 1;
                        else notHelpfulDelta += 1;

                        existingVote.VoteType = desired.Value;
                        existingVote.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                    }
                }

                if (helpfulDelta != 0 || notHelpfulDelta != 0)
                {
                    userReview.HelpfulCount = Math.Max(0, userReview.HelpfulCount + helpfulDelta);
                    userReview.NotHelpfulCount = Math.Max(0, userReview.NotHelpfulCount + notHelpfulDelta);
                }

                VoteType? current = desired;

                return OperationResult<(VoteType?, int, int)>.Success((current, userReview.HelpfulCount, userReview.NotHelpfulCount), "Değerlendirme güncellendi.");
            }, ct: ct);

            VoteType? current = null;

            if (result.IsSuccess)
            {
                current = result.Data.Item1;

                _logger.LogInformation(
                    "User review vote set. ReviewId: {ReviewId}, UserId: {UserId}, Desired: {Desired}, Current: {Current}",
                    userReviewId, userId, desired, current);
            }

            return result;
        }
    }
}
