using Application.Common.Models;
using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Extensions;
using ETicaret.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ETicaret.Controllers
{
    public class AccountController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IAuthService _authService;
        private readonly IProductService _productService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ISecurityLogService _securityLogService;
        private readonly IEmailService _emailService;
        private readonly ICaptchaService _captchaService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IUserService _userService;

        public AccountController(
            ICartService cartService,
            IAuthService authService,
            IProductService productService,
            INotificationService notificationService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ISecurityLogService securityLogService,
            IEmailService emailService,
            ICaptchaService captchaService,
            IConfiguration configuration,
            IWebHostEnvironment env,
            IUserService userService)
        {
            _cartService = cartService;
            _authService = authService;
            _productService = productService;
            _notificationService = notificationService;
            _userManager = userManager;
            _signInManager = signInManager;
            _securityLogService = securityLogService;
            _emailService = emailService;
            _captchaService = captchaService;
            _configuration = configuration;
            _env = env;
            _userService = userService;
        }

        public IActionResult Login([FromQuery(Name = "ReturnUrl")] string ReturnUrl = "/")
        {
            var model = new UserModel
            {
                ReturnUrl = ReturnUrl,
            };

            ViewData["RecaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromForm] UserModel model)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];

            if (model?.Login == null || !ModelState.IsValid)
                return View(model);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var result = await _authService.AuthenticateAsync(new AuthRequest
            {
                Email = model.Login.Email,
                Password = model.Login.Password,
                RememberMe = model.Login.RememberMe,
                IpAddress = ipAddress,
                CaptchaToken = model.Login.CaptchaToken,
                SkipCaptcha = _env.IsDevelopment()
            });

            if (!result.Succeeded)
            {
                AddModelErrorFromResult(result);
                return View(model);
            }

            var claims = await _authService.BuildClaimsAsync(result.UserId!);
            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity));

            var sessionCartDto = SessionCart.GetCartDto(HttpContext.Session);
            var mergedCart = await _cartService.MergeCartsAsync(result.UserId!, sessionCartDto);
            HttpContext.Session.SetJson("cart", mergedCart);

            await SetFavouriteProductsCookie(result.UserId!);

            return Redirect(model.ReturnUrl ?? "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromForm] UserModel model)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];

            if (model.Register is null || !ModelState.IsValid)
            {
                model.IsRegister = true;
                return View("Login", model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var confirmationLinkTemplate = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = "{userId}", token = "{token}" },
                protocol: Request.Scheme)!;

            var dto = new RegisterDto
            {
                Email = model.Register.Email,
                Password = model.Register.Password,
                FirstName = model.Register.FirstName,
                LastName = model.Register.LastName,
                PhoneNumber = model.Register.PhoneNumber,
                BirthDate = model.Register.BirthDate,
                AcceptTerms = model.Register.AcceptTerms,
                AcceptMarketing = model.Register.AcceptMarketing
            };

            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                RegisterDto = dto,
                IpAddress = ipAddress,
                CaptchaToken = model.Register.CaptchaToken,
                SkipCaptcha = _env.IsDevelopment(),
                ConfirmationLinkTemplate = confirmationLinkTemplate
            });

            if (!result.Succeeded)
            {
                AddModelErrorFromRegisterResult(result);
                model.IsRegister = true;
                return View("Login", model);
            }

            TempData["toastContent"] = "Kayıt başarılı! Lütfen e-postanızı kontrol edin.";
            TempData["toastType"] = "success";
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout([FromQuery(Name = "ReturnUrl")] string ReturnUrl = "/")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);

            if (userId is not null && userName is not null)
            {
                Response.Cookies.Delete("FavouriteProducts");
                HttpContext.Session.Clear();

                await _signInManager.SignOutAsync();

                await _authService.LogoutAsync(userId, userName);
            }

            return Redirect(ReturnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["toastContent"] = "Geçersiz doğrulama linki.";
                TempData["toastType"] = "error";
                return RedirectToAction("Login");
            }

            var result = await _authService.ConfirmEmailAsync(userId, token);

            if (result.Succeeded)
            {
                TempData["toastContent"] = "E-posta adresiniz başarıyla doğrulandı!";
                TempData["toastType"] = "success";
                return RedirectToAction("Login");
            }

            var message = result.UserNotFound
                ? "Kullanıcı bulunamadı."
                : "E-posta doğrulama başarısız. Lütfen tekrar deneyin.";

            TempData["toastContent"] = message;
            TempData["toastType"] = "error";
            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Impersonate(string userId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _authService.StartImpersonationAsync(
                targetUserId: userId,
                adminId: adminId);

            if (!result.Succeeded)
            {
                TempData["toastContent"] = result.ErrorMessage;
                TempData["toastType"] = "error";
                return Redirect("/admin/users");
            }

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("FavouriteProducts");

            var claimsIdentity = new ClaimsIdentity(result.Claims!, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return Redirect("/");
        }

        [Authorize]
        [HttpPost]
        [ActionName("stop-impersonation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopImpersonation()
        {
            var isImpersonating = User.FindFirstValue("IsImpersonating");
            if (isImpersonating != "true")
            {
                return Redirect("/");
            }

            var originalAdminId = User.FindFirstValue("OriginalAdminId");
            if (string.IsNullOrEmpty(originalAdminId))
            {
                await _signInManager.SignOutAsync();
                return Redirect("/account/login");
            }

            var result = await _authService.StopImpersonationAsync(originalAdminId);

            if (!result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/account/login");
            }

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("FavouriteProducts");

            var claimsIdentity = new ClaimsIdentity(result.Claims!, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity));

            TempData["toastContent"] = "Admin oturumuna geri dönüldü.";
            TempData["toastType"] = "success";

            return Redirect("/admin/users");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Orders()
        {
            return View();
        }

        [Authorize]
        public IActionResult Reviews()
        {
            return View();
        }

        [Authorize]
        public IActionResult Addresses()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Favourites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userService.GetOneUsersFavouritesAsync(userId!);
            ViewBag.FavouriteIds = user.FavouriteProductVariantsId;

            UpdateFavoritesCookie(user.FavouriteProductVariantsId);

            var favouriteProducts = await _productService.GetFavouritesAsync(user);

            return View(favouriteProducts);
        }

        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetByUserIdAsync(userId!);

            return View(notifications);
        }

        // Helpers
        private void AddModelErrorFromResult(AuthResult result)
        {
            var errorKey = result.InvalidCredentials ? "Login.Name" : string.Empty;

            var message = result switch
            {
                { CaptchaFailed: true } => "CAPTCHA doğrulaması başarısız.",
                { IpBlocked: true } => "Çok fazla başarısız giriş denemesi tespit edildi. Lütfen 15 dakika sonra tekrar deneyin.",
                { RequiresEmailConfirmation: true } => "E-posta adresinizi doğrulamanız gerekmektedir.",
                { IsDeleted: true } => "Bu hesap silinmiştir.",
                { IsLockedOut: true, RemainingLockoutMinutes: > 0 } => $"Hesabınız {result.RemainingLockoutMinutes} dakika daha kilitli.",
                { IsLockedOut: true } => "Hesabınız çok fazla başarısız giriş denemesi nedeniyle kilitlenmiştir. Lütfen daha sonra tekrar deneyin.",
                { InvalidCredentials: true } => "Kullanıcı adı veya şifre hatalı.",
                _ => "Bilinmeyen bir hata oluştu."
            };

            ModelState.AddModelError(errorKey, message);
        }

        private async Task SetFavouriteProductsCookie(string userId)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = Request.IsHttps,
                IsEssential = true
            };

            var favouriteIds = await _userService.GetOneUsersFavouritesAsync(userId);


            Response.Cookies.Delete("FavouriteProducts");

            if (favouriteIds.FavouriteProductVariantsId != null && favouriteIds.FavouriteProductVariantsId.Count != 0)
            {
                var favouriteProductVariantIdsString = string.Join("|", favouriteIds);
                Response.Cookies.Append("FavouriteProducts", favouriteProductVariantIdsString, cookieOptions);
            }
        }

        private void AddModelErrorFromRegisterResult(RegisterResult result)
        {
            if (result.CaptchaFailed)
            {
                ModelState.AddModelError(string.Empty, "CAPTCHA doğrulaması başarısız.");
                return;
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
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
    }
}

