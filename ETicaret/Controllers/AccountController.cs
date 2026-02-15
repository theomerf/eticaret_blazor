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
using System.Net;
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
            IWebHostEnvironment env)
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
            {
                return View(model);
            }

            if (!_env.IsDevelopment() && !await _captchaService.ValidateAsync(model.Login.CaptchaToken))
            {
                ModelState.AddModelError("", "CAPTCHA doğrulaması başarısız.");

                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (await _securityLogService.IsIpBlockedAsync(ipAddress))
            {
                await _securityLogService.LogFailedLoginAsync(
                    email: model.Login.Email,
                    failureReason: "IP blocked due to too many failed attempts"
                );

                ModelState.AddModelError("", "Çok fazla başarısız giriş denemesi tespit edildi. Lütfen 15 dakika sonra tekrar deneyin.");

                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Login.Email);

            if (user != null && user.Email != null && user.UserName != null)
            {
                if (!user.EmailConfirmed)
                {
                    ModelState.AddModelError("", "E-posta adresinizi doğrulamanız gerekmektedir.");

                    return View(model);
                }

                if (user.IsDeleted)
                {
                    ModelState.AddModelError("", "Bu hesap silinmiştir.");

                    return View(model);
                }

                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    var remainingTime = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                    ModelState.AddModelError("", $"Hesabınız {remainingTime.Minutes} dakika daha kilitli.");

                    return View(model);
                }

                await _signInManager.SignOutAsync();

                var result = await _signInManager.PasswordSignInAsync(
                    model.Login.Email,
                    model.Login.Password,
                    model.Login.RememberMe,
                    lockoutOnFailure: true
                );

                if (result.Succeeded)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    user.LastLoginIpAddress = ipAddress;
                    user.LastFailedLoginDate = null;
                    await _userManager.UpdateAsync(user);

                    await _userManager.ResetAccessFailedCountAsync(user);

                    var sessionCartDto = SessionCart.GetCartDto(HttpContext.Session);
                    var mergedCart = await _cartService.MergeCartsAsync(user.Id, sessionCartDto);
                    HttpContext.Session.SetJson("cart", mergedCart);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim("first_name", user.FirstName),
                        new Claim("last_name", user.LastName),
                        new Claim("identity_number", user.IdentityNumber)
                    };

                    var roles = await _userManager.GetRolesAsync(user);
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    if (user.FavouriteProductVariantsId != null && user.FavouriteProductVariantsId.Any())
                    {
                        var favouriteProductVariantIdsString = string.Join("|", user.FavouriteProductVariantsId);
                        Response.Cookies.Append("FavouriteProducts", favouriteProductVariantIdsString, new CookieOptions
                        {
                            Expires = DateTimeOffset.Now.AddYears(1),
                            Path = "/",
                            SameSite = SameSiteMode.Lax,
                            HttpOnly = false,
                            Secure = Request.IsHttps,
                            IsEssential = true
                        });
                    }
                    else
                    {
                        Response.Cookies.Delete("FavouriteProducts");
                        Response.Cookies.Append("FavouriteProducts", "", new CookieOptions
                        {
                            Expires = DateTimeOffset.Now.AddYears(1),
                            Path = "/",
                            SameSite = SameSiteMode.Lax,
                            HttpOnly = false,
                            Secure = Request.IsHttps,
                            IsEssential = true
                        });
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                    await HttpContext.SignInAsync(
                        IdentityConstants.ApplicationScheme,
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    await _securityLogService.LogLoginAsync(
                        userId: user.Id,
                        userName: user.UserName!,
                        email: user.Email!,
                        isSuccess: true
                    );

                    return Redirect(model.ReturnUrl ?? "/");
                }
                else if (result.IsLockedOut)
                {
                    await _securityLogService.LogFailedLoginAsync(
                        email: model.Login.Email,
                        failureReason: "Account locked out"
                    );

                    ModelState.AddModelError("", "Hesabınız çok fazla başarısız giriş denemesi nedeniyle kilitlenmiştir. Lütfen daha sonra tekrar deneyin.");

                    return View(model);
                }
                else
                {
                    user!.LastFailedLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    ModelState.AddModelError("Login.Name", "Kullanıcı adı veya şifre hatalı.");
                }
            }
            else
            {
                ModelState.AddModelError("Login.Name", "Kullanıcı adı veya şifre hatalı.");
            }

            await _securityLogService.LogFailedLoginAsync(
                email: model.Login.Email,
                failureReason: "Invalid credentials"
            );

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout([FromQuery(Name = "ReturnUrl")] string ReturnUrl = "/")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);

            var currentCart = SessionCart.GetCartDto(HttpContext.Session);

            if (userId != null && userName != null)
            {
                Response.Cookies.Delete("FavouriteProducts");
                HttpContext.Session.Clear();

                await _signInManager.SignOutAsync();
                await _securityLogService.LogLogoutAsync(userId, userName);
            }

            return Redirect(ReturnUrl);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Impersonate(string userId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Prevent self-impersonation
            if (adminId == userId)
            {
                TempData["toastContent"] = "Kendi hesabınıza geçiş yapamazsınız.";
                TempData["toastType"] = "error";
                return RedirectToAction("Index", "Account");
            }

            var result = await _authService.BuildImpersonationClaimsAsync(userId, adminId);
            if (!result.IsSuccess)
            {
                TempData["toastContent"] = result.Message;
                TempData["toastType"] = "error";
                return Redirect("/admin/users");
            }

            // Sign out admin
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("FavouriteProducts");

            // Sign in as target user with impersonation claims
            var claimsIdentity = new ClaimsIdentity(result.Data!, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );
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
            var impersonatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(originalAdminId))
            {
                await _signInManager.SignOutAsync();
                return Redirect("/account/login");
            }

            var adminUser = await _userManager.FindByIdAsync(originalAdminId);
            if (adminUser == null)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/account/login");
            }

            // Sign out impersonated session
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("FavouriteProducts");

            // Rebuild admin claims (same as Login flow)
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminUser.UserName ?? adminUser.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, adminUser.Id),
                new Claim("first_name", adminUser.FirstName),
                new Claim("last_name", adminUser.LastName),
                new Claim("identity_number", adminUser.IdentityNumber ?? "")
            };

            var adminRoles = await _userManager.GetRolesAsync(adminUser);
            foreach (var role in adminRoles)
            {
                adminClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(adminClaims, IdentityConstants.ApplicationScheme);
            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );

            // Audit log stop
            await _securityLogService.LogLoginAsync(
                userId: adminUser.Id,
                userName: adminUser.UserName!,
                email: adminUser.Email!,
                isSuccess: true
            );

            TempData["toastContent"] = "Admin oturumuna geri dönüldü.";
            TempData["toastType"] = "success";

            return Redirect("/admin/users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromForm] UserModel model)
        {
            ViewData["RecaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];

            if (model.Register == null || !ModelState.IsValid)
            {
                model.IsRegister = true;
                return View("Login", model);
            }

            if (!_env.IsDevelopment() && !await _captchaService.ValidateAsync(model.Register.CaptchaToken))
            {
                ModelState.AddModelError("", "CAPTCHA doğrulaması başarısız.");
                model.IsRegister = true;
                return View("Login", model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var user = new User
            {
                UserName = model.Register!.Email,
                FirstName = model.Register!.FirstName,
                LastName = model.Register!.LastName,
                PhoneNumber = model.Register!.PhoneNumber,
                BirthDate = model.Register!.BirthDate,
                Email = model.Register!.Email,
                RegistrationIpAddress = ipAddress,
                AcceptedTerms = model.Register!.AcceptTerms,
                TermsAcceptedDate = DateTime.UtcNow,
                AcceptedMarketing = model.Register!.AcceptMarketing,
                MarketingAcceptedDate = model.Register.AcceptMarketing ? DateTime.UtcNow : null,
            };

            var result = await _userManager.CreateAsync(user, model.Register!.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");

                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = emailToken },
                    protocol: Request.Scheme
                );

                await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink!);

                if (roleResult.Succeeded)
                {
                    TempData["toastContent"] = "Kayıt başarılı! Lütfen e-postanızı kontrol edin.";
                    TempData["toastType"] = "success";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.IsRegister = true;
            }

            return View("Login", model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("Geçersiz doğrulama linki.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded && user.Email != null)
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

                TempData["toastContent"] = "E-posta adresiniz başarıyla doğrulandı!";
                TempData["toastType"] = "success";
                return RedirectToAction("Login");
            }

            TempData["toastContent"] = "E-posta doğrulama başarısız. Lütfen tekrar deneyin.";
            TempData["toastType"] = "error";

            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied([FromQuery(Name = "ReturnUrl")] string returnUrl)
        {
            return View();
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
        public async Task<IActionResult> Favourites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _authService.GetOneUsersFavouritesAsync(userId!);
            ViewBag.FavouriteIds = user.FavouriteProductVariantsId;

            UpdateFavoritesCookie(user.FavouriteProductVariantsId);

            var favouriteProducts = await _productService.GetFavouritesAsync(user);

            return View(favouriteProducts);
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetByUserIdAsync(userId!);

            return View(notifications);
        }
    }
}

