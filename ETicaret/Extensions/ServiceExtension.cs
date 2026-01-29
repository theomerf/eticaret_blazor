using Application.Common.Security;
using Application.Repositories.Interfaces;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Extensions;
using ETicaret.Models;
using ETicaret.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories.Implementations;
using Infrastructure.Security;
using Infrastructure.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace ETicaret.Extensions
{
    public static class ServiceExtension
    {
        public static void ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<RepositoryContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("mssqlconnection"),
                b => b.MigrationsAssembly("ETicaret"));

                options.EnableSensitiveDataLogging(true);
            });
        }

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedAccount = false;

                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddEntityFrameworkStores<RepositoryContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(24); // Email confirmation token 24 saat geçerli
            });
        }

        public static void ConfigureSession(this IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "ETicaret.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax; // Blazor Server için gerekli
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
            services.AddScoped<Cart>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var session = httpContextAccessor.HttpContext?.Session;

                SessionCart cart = session?.GetJson<SessionCart>("cart") ?? new SessionCart();
                cart.Session = session;

                return cart;
            });
        }

        public static void ConfigureRepositoryRegistration(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUserReviewRepository, UserReviewRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISecurityLogRepository, SecurityLogRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
        }

        public static void ConfigureServiceRegistration(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<IOrderService, OrderManager>();
            services.AddScoped<IAuthService, AuthManager>();
            services.AddScoped<IUserReviewService, UserReviewManager>();
            services.AddScoped<ICartService, CartManager>();
            services.AddScoped<Application.Services.Interfaces.INotificationService, NotificationManager>();
            services.AddScoped<Services.INotificationService, NotificationService>();
            services.AddScoped<ICartStateService, CartStateService>();
            services.AddScoped<IFavouriteStateService, FavouriteStateService>();
            services.AddScoped<IAuditLogService, AuditLogManager>();
            services.AddScoped<ISecurityLogService, SecurityLogManager>();
            services.AddScoped<IFileService, FileManager>();
            services.AddScoped<IEmailService, EmailManager>();
            services.AddScoped<ICaptchaService, CaptchaManager>();
            services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerManager>();
            services.AddHttpClient();
            services.AddScoped<IAddressService, AddressManager>();
        }
        public static void ConfigureApplicationCookie(this IServiceCollection services)
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/accessDenied";
                options.LogoutPath = "/account/logout";

                options.Cookie.Name = "ETicaret.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.IsEssential = true;

                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;

                options.Events.OnSigningIn = context =>
                {
                    if (context.Properties.IsPersistent)
                    {
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                        context.Properties.AllowRefresh = true;
                        context.Properties.IsPersistent = true;
                    }
                    else
                    {
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
                        context.Properties.AllowRefresh = true;
                        context.Properties.IsPersistent = false;
                    }

                    return Task.CompletedTask;
                };
            });

        }

        public static void ConfigureRouting(this IServiceCollection services)
        {
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.AppendTrailingSlash = false;
            });
        }

        public static void ConfigureCsv(this IServiceCollection services)
        {
            services.AddScoped<ImportFromCsvExtension>();
        }

        public static void ConfigureBaseApiAdress(this IServiceCollection services)
        {
            services.AddHttpClient("ApiClient")
                .ConfigureHttpClient((sp, client) =>
                {
                    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                    var request = httpContextAccessor.HttpContext?.Request;

                    if (request != null)
                    {
                        var baseUri = $"{request.Scheme}://{request.Host}";
                        client.BaseAddress = new Uri(baseUri);
                    }
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler { UseCookies = true });
        }
    }
}
