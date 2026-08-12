namespace Microsoft.Extensions.DependencyInjection;

using System.Text;

using FirebaseAdmin;

using Google.Apis.Auth.OAuth2;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Infrastructure.Auth;
using Taxi.Infrastructure.BackgroundJobs;
using Taxi.Infrastructure.Common;
using Taxi.Infrastructure.Data;
using Taxi.Infrastructure.Data.Interceptors;
using Taxi.Infrastructure.Email;
using Taxi.Infrastructure.Identity;
using Taxi.Infrastructure.Incidents;
using Taxi.Infrastructure.Maps;
using Taxi.Infrastructure.Notifications;
using Taxi.Infrastructure.Outbox;
using Taxi.Infrastructure.Payments;
using Taxi.Infrastructure.RealTime;
using Taxi.Infrastructure.Services.Invoices;
using Taxi.Infrastructure.Settings;
using Taxi.Infrastructure.Sms;
using Taxi.Infrastructure.Storage;
using Taxi.Infrastructure.Wallet;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<InvoiceIssuerOptions>(configuration.GetSection(InvoiceIssuerOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<RefundReconciliationOptions>(configuration.GetSection(RefundReconciliationOptions.SectionName));
        services.Configure<WalletOptions>(configuration.GetSection(WalletOptions.SectionName));
        services.Configure<PaymentPreferenceOptions>(configuration.GetSection(PaymentPreferenceOptions.SectionName));
        services.Configure<CmComSmsOptions>(configuration.GetSection(CmComSmsOptions.SectionName));
        services.Configure<TitanEmailOptions>(configuration.GetSection(TitanEmailOptions.SectionName));

        if (FirebaseApp.DefaultInstance == null)
        {
            var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>()
                ?? throw new InvalidOperationException("AppSettings configuration section is missing.");

            var raw = appSettings.FirebaseCredentials;
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException(
                    "AppSettings:FirebaseCredentials is missing. " +
                    "Provide inline JSON (docker/.env) or a file path (user-secrets).");
            }

            var credential = raw.TrimStart().StartsWith('{')
                ? CredentialFactory.FromJson<ServiceAccountCredential>(raw).ToGoogleCredential()
                : CredentialFactory.FromFile<ServiceAccountCredential>(raw).ToGoogleCredential();

            FirebaseApp.Create(new AppOptions { Credential = credential });
        }

        services.AddSingleton(TimeProvider.System);
        services.AddSignalR();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditLogInterceptor>();

        // Registered last so it adds OutboxMessage rows after audit scanning has run
        // (AuditLogInterceptor already ignores OutboxMessage, so ordering is safe either way).
        services.AddScoped<ISaveChangesInterceptor, ConvertDomainEventsToOutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(60);
                });
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("JwtSettings");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
            };

            // SignalR JWT auth: WebSocket clients cannot send Authorization headers,
            // so we accept the access token from the ?access_token= query string
            // when the request targets a hub.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services
        .AddIdentityCore<AppUser>(options =>
        {
            // Password complexity is intentionally disabled — owner decision, so any
            // password is accepted for admin accounts. Brute-force protection therefore
            // rests entirely on the lockout below and the per-caller rate limiter.
            options.Password.RequiredLength = 1;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 1;
            options.SignIn.RequireConfirmedAccount = false;

            // Throttle credential stuffing against admin login at the account level; the
            // per-caller rate limiter only bounds a single IP/user partition.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<ITokenProvider, TokenProvider>();
        services.AddScoped<ISessionRevoker, SessionRevoker>();

        services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

        // Backend-owned OTP + external providers (CM.com SMS, Titan SMTP). Providers are
        // isolated behind interfaces so they can be replaced without touching auth logic.
        services.AddSingleton<IOtpCodeHasher, OtpCodeHasher>();
        services.AddSingleton<IRegistrationTokenService, RegistrationTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailSender, TitanEmailSender>();
        services.AddScoped<IWelcomeEmailService, WelcomeEmailService>();
        services.AddHttpClient<ISmsSender, CmComSmsSender>((sp, client) =>
        {
            var smsOptions = sp.GetRequiredService<IOptions<CmComSmsOptions>>().Value;
            client.BaseAddress = new Uri(smsOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<IDirectionsService, GoogleMapsService>(client =>
        {
            client.BaseAddress = new Uri("https://maps.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<IGeocodingService, GoogleGeocodingService>(client =>
        {
            client.BaseAddress = new Uri("https://maps.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ITripNotifier, SignalRTripNotifier>();
        services.AddScoped<ICustomerIncidentRecorder, CustomerIncidentRecorder>();
        services.AddScoped<IDriverLocationNotifier, SignalRDriverLocationNotifier>();
        services.AddSingleton<PickupRouteCache>();
        services.AddScoped<INotificationService, FcmNotificationService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddScoped<IStripeWebhookValidator, StripeWebhookValidator>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddSingleton<IClientConfigProvider, ClientConfigProvider>();
        services.AddSingleton<IRefundProcessingOptionsProvider, RefundProcessingOptionsProvider>();

        services.AddScoped<IInvoiceNumberGenerator, SequentialInvoiceNumberGenerator>();
        services.AddScoped<IInvoiceIssuanceService, InvoiceIssuanceService>();
        services.AddSingleton<IInvoicePdfRenderer, InvoicePdfRenderer>();

        services.AddHybridCache(options => options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.FromSeconds(30),
        });

        // Registered first so upload-area folders exist (and writability is verified)
        // before any other hosted service or request runs — IHostedService.StartAsync
        // executes in registration order.
        services.AddHostedService<StorageInitializer>();
        services.AddHostedService<ScheduledTripActivationService>();
        services.AddHostedService<NoDriverDetectionService>();
        services.AddHostedService<PendingTripEditExpiryService>();
        services.AddHostedService<OutboxDispatcherService>();
        services.AddHostedService<TripChatCleanupService>();
        services.AddHostedService<OtpCleanupService>();
        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<RefundPendingReconciliationService>();

        return services;
    }
}
