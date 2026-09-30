using Amazon.S3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Infrastructure.BackgroundJobs;
using PMIS.Infrastructure.Caching;
using PMIS.Infrastructure.Mongo;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Queries;
using PMIS.Infrastructure.Reporting;
using PMIS.Infrastructure.Repositories;
using PMIS.Infrastructure.Security;
using PMIS.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = new[] { "text/css", "application/javascript", "text/javascript" };
});

builder.Services.AddDbContext<PmisDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PmisDatabase"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));
builder.Services.AddSingleton<PmisMongoContext>();
builder.Services.AddScoped<AuditLogRepository>();
builder.Services.AddScoped<IAuditLogRepository>(sp => new NotifyingAuditLogRepository(
    sp.GetRequiredService<AuditLogRepository>(),
    sp.GetRequiredService<INotificationService>(),
    sp.GetRequiredService<ILogger<NotifyingAuditLogRepository>>()));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
builder.Services.AddScoped<INotificationQueryService, NotificationQueryService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRequestQueryService, RequestQueryService>();
builder.Services.AddScoped<IRequestAggregateQueryService, RequestAggregateQueryService>();
builder.Services.AddScoped<IAdminUserQueryService, AdminUserQueryService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IUserProfileQueryService, UserProfileQueryService>();
builder.Services.AddScoped<IUserPhotoReadService, UserPhotoReadService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<ILogRetentionService, LogRetentionService>();
builder.Services.AddScoped<IAccountSettingsService, AccountSettingsService>();
builder.Services.AddScoped<IUserPhotoUploadService, UserPhotoUploadService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRequestActionService, RequestActionService>();
builder.Services.AddScoped<IRejectionReasonService, RejectionReasonService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<ITargetSystemService, TargetSystemService>();
builder.Services.AddScoped<IRequestAttachmentService, RequestAttachmentService>();
builder.Services.AddScoped<IRequestCreationService, RequestCreationService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IReportExcelBuilder, ReportExcelBuilder>();
builder.Services.AddScoped<IReportPdfBuilder, ReportPdfBuilder>();
builder.Services.AddSingleton<ReportGenerationLimiter>();

builder.Services.AddHostedService<DailyDigestService>();

builder.Services.AddHostedService<LogRetentionSweepService>();

builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MinioSettings>>().Value;
    var config = new AmazonS3Config
    {
        ServiceURL = $"{(settings.UseSsl ? "https" : "http")}://{settings.Endpoint}",
        ForcePathStyle = true
    };
    return new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
});
builder.Services.AddSingleton<MinioBucketProvisioner>();
builder.Services.AddScoped<IAttachmentStorage, MinioAttachmentStorage>();
builder.Services.AddScoped<IUserPhotoStorage, MinioUserPhotoStorage>();

builder.Services.Configure<ClamAvSettings>(builder.Configuration.GetSection("ClamAv"));
builder.Services.AddScoped<IVirusScanner, ClamAvScanner>();

var redisConnection = await ConnectionMultiplexer.ConnectAsync(
    builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured."));
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

builder.Services.AddSingleton<IReferenceDataCache, RedisReferenceDataCache>();

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redisConnection, "pmis:dataprotection-keys")
    .SetApplicationName("PMIS");

builder.Services.Configure<NafathSettings>(builder.Configuration.GetSection("Nafath"));

var nafathBypassEnabled = builder.Environment.IsDevelopment()
    && !string.IsNullOrWhiteSpace(builder.Configuration["Nafath:BypassNationalId"]);
if (nafathBypassEnabled)
{
    builder.Services.AddScoped<INafathSsoClient, NafathBypassSsoClient>();
}
else
{
    builder.Services.AddHttpClient<INafathSsoClient, NafathSsoClient>((sp, client) =>
    {
        var settings = sp.GetRequiredService<IOptions<NafathSettings>>().Value;
        client.BaseAddress = new Uri(settings.IamSsoUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
    builder.Services.PostConfigure<NafathSettings>(settings => settings.BypassNationalId = null);
}

var allowInsecureCookie = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Auth:AllowInsecureCookie");

builder.Services.Configure<EmailSmsSettings>(builder.Configuration.GetSection("EmailSms"));
builder.Services.AddHttpClient<IEmailSender, LmsEmailSender>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<EmailSmsSettings>>().Value;
    client.BaseAddress = new Uri(settings.LmsBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient<ISmsSender, LmsSmsSender>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<EmailSmsSettings>>().Value;
    client.BaseAddress = new Uri(settings.LmsBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IOtpService, OtpService>();

builder.Services.AddScoped<INafathLoginStateStore, RedisNafathLoginStateStore>();
builder.Services.AddScoped<ILoginLockoutService, RedisLoginLockoutService>();

builder.Services.AddScoped<INafathPendingProfileStore, RedisNafathPendingProfileStore>();

builder.Services.AddScoped<INafathSsoCoordinator, NafathSsoCoordinator>();

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = allowInsecureCookie
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error");

app.UseHttpsRedirection();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/assets"),
    branch => branch.UseResponseCompression());

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var isFont = ctx.File.Name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
            || ctx.File.Name.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
            || ctx.File.Name.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase);

        var cacheControl = isFont
            ? "public, max-age=31536000, immutable"
            : "public, max-age=86400";

        ctx.Context.Response.Headers.CacheControl = cacheControl;
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}");

await app.RunAsync();
