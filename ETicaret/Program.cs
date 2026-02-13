using Application.Mappings;
using ETicaret.Extensions;
using ETicaret.Middlewares;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ETicaret web application...");

    var columnWriters = new Dictionary<string, ColumnWriterBase>
    {
        { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
        { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
        { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
        { "time_stamp", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
        { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
        { "properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) }
    };

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .WriteTo.PostgreSQL(
            connectionString: builder.Configuration.GetConnectionString("logdb")!,
            tableName: "serilog_logs",
            columnOptions: columnWriters,
            needAutoCreateTable: true,
            restrictedToMinimumLevel: LogEventLevel.Warning,
            batchSizeLimit: 50,
            period: TimeSpan.FromSeconds(5)
        )
        .CreateLogger();

    builder.Host.UseSerilog();

    if (!builder.Environment.IsDevelopment())
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    builder.Services.AddHttpContextAccessor();

    builder.Services.ConfigureDbContext(builder.Configuration, builder.Environment);
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
        app.UseExceptionHandler("/home/error");
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
