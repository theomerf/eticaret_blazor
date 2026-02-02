using Application.Common.Options;
using Application.Mappings;
using ETicaret.Extensions;
using ETicaret.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

try
{
    Log.Information("Starting ETicaret web application...");

    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    builder.Host.UseSerilog();

    builder.Services.AddHttpContextAccessor();

    builder.Services.ConfigureDbContext(builder.Configuration);
    builder.Services.ConfigureIdentity();
    builder.Services.ConfigureSession();
    builder.Services.ConfigureRepositoryRegistration();
    builder.Services.ConfigureServiceRegistration();
    builder.Services.ConfigureRouting();
    builder.Services.ConfigureApplicationCookie();
    builder.Services.ConfigureRateLimiting();
    builder.Services.AddMemoryCache();
    builder.Services.ConfigureCsv();
    builder.Services.ConfigureBaseApiAdress();
    builder.Services.ConfigureFileStorageService(builder.Configuration);
    builder.Services.ConfigurePaymentServices(builder.Configuration);

    builder.Services.AddAutoMapper(typeof(MappingProfile));

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    builder.Services.AddServerSideBlazor(options =>
    {
        options.DetailedErrors = true;
    });

    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

    var app = builder.Build();

    app.UseGlobalExceptionHandler(app.Environment.IsDevelopment());

    app.UseRateLimiter();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
            diagnosticContext.Set("UserId", httpContext.User?.Identity?.Name);
        };
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();

    app.UseRouting();

    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapBlazorHub();
    app.MapRazorPages();
    app.MapFallbackToPage("/admin/{**path}", "/_Host");

    app.ConfigureAndCheckMigration();
    app.ConfigureLocalization();
    await app.ConfigureDefaultAdminUser();
    await app.ConfigureCsv();

    Log.Information("ETicaret web application started successfully.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
