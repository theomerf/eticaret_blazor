using Application.DTOs;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ClientModel.Primitives;
using System.Security.Claims;

namespace ETicaret.Controllers.Api
{
    [ApiController]
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IAuthService _authService;
        private readonly IUserReviewService _userReviewService;
        private readonly IFileService _fileService;

        public AccountApiController(INotificationService notificationService, IAuthService authService, IUserReviewService userReviewService, IFileService fileService)
        {
            _notificationService = notificationService;
            _authService = authService;
            _userReviewService = userReviewService;
            _fileService = fileService;
        }

        [Authorize]
        [HttpGet("favourites/count")]
        public async Task<IActionResult> GetFavouritesCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            int favouritesCount = 0;

            if (userId != null)
            {
                var user = await _authService.GetOneUsersFavouritesAsync(userName!);
                favouritesCount = user.FavouriteProductsId.Count();
            }

            return Ok(new { count = favouritesCount });
        }

        [Authorize]
        [HttpGet("notifications/count")]
        public async Task<IActionResult> GetNotificationsCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetAllNotificationsOfOneUserAsync(userId!);
            var unreadNotifications = notifications.Where(n => n.IsRead == false);

            return Ok(new { count = unreadNotifications.Count() });
        }

        private void UpdateFavoritesCookie(ICollection<int> favoriteIds)
        {
            var cookieValue = favoriteIds.Any()
                ? string.Join("|", favoriteIds)
                : string.Empty;

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = Request.IsHttps,
                IsEssential = true
            };

            if (string.IsNullOrEmpty(cookieValue))
            {
                Response.Cookies.Append("FavouriteProducts", "", cookieOptions);
            }
            else
            {
                Response.Cookies.Append("FavouriteProducts", cookieValue, cookieOptions);
            }
        }

        [Authorize]
        [HttpPost("favourites/add/{productId:int}")]
        public async Task<IActionResult> AddToFavourites(int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Geçersiz ürün ID.",
                    type = "danger"
                });
            }

            var result = await _authService.AddToFavouritesAsync(productId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            UpdateFavoritesCookie(result.Data!.FavouriteProductsId);

            return Ok(new
            {
                success = true,
                message = result.Message,
                type = result.Type,
                count = result.Data.FavouriteProductsId.Count
            });
        }

        [Authorize]
        [HttpPost("favourites/remove/{productId:int}")]
        public async Task<IActionResult> RemoveFromFavourites(int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Geçersiz ürün ID.",
                    type = "danger"
                });
            }

            var result = await _authService.RemoveFromFavouritesAsync(productId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            UpdateFavoritesCookie(result.Data!.FavouriteProductsId);

            return Ok(new
            {
                success = true,
                message = result.Message,
                type = result.Type,
                count = result.Data.FavouriteProductsId.Count
            });
        }

        [Authorize]
        [HttpPut("notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var result = await _notificationService.MarkAllNotificationsAsReadAsync();

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                type = result.Type
            });
        }

        [Authorize]
        [HttpDelete("notifications/remove-all")]
        public async Task<IActionResult> RemoveAllNotifications()
        {
            var result = await _notificationService.RemoveAllNotificationsAsync();

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.Message,
                type = result.Type
            });
        }

        [Authorize]
        [HttpDelete("notifications/remove/{notificationId:int}")]
        public async Task<IActionResult> RemoveNotification([FromRoute] int notificationId)
        {
            var result = await _notificationService.RemoveNotificationAsync(notificationId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.Message,
                type = result.Type
            });
        }

        [Authorize]
        [HttpPut("notifications/mark-read/{notificationId:int}")]
        public async Task<IActionResult> MarkNotificationAsRead([FromRoute] int notificationId)
        {
            var result = await _notificationService.MarkNotificationAsReadAsync(notificationId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type,
                });
            }

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.Message,
                type = result.Type
            });
        }

        [Authorize]
        [HttpPost("create-review")]
        public async Task<IActionResult> CreateReview([FromForm] UserReviewDtoForCreation reviewDto, IFormFile? file)
        {
            if (file != null)
            {
                using var fileReadStream = file.OpenReadStream();

                var uploadResult = await _fileService.UploadAsync(fileReadStream, file.FileName, file.ContentType, "reviews");

                if (uploadResult.IsSuccess)
                {
                    reviewDto.ReviewPictureUrl = uploadResult.Data;

                    var result = await _userReviewService.CreateUserReviewAsync(reviewDto);

                    if (!result.IsSuccess)
                    {
                        return StatusCode(500, new
                        {
                            success = false,
                            message = result.Message,
                            type = result.Type,
                        });
                    }

                    return Ok(new
                    {
                        success = result.IsSuccess,
                        message = result.Message,
                        type = result.Type
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = uploadResult.Message,
                        type = uploadResult.Type,
                    });
                }
            }
            else
            {
                var result = await _userReviewService.CreateUserReviewAsync(reviewDto);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = result.Message,
                        type = result.Type,
                    });
                }

                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.Message,
                    type = result.Type
                });
            }
        }
    }
}
