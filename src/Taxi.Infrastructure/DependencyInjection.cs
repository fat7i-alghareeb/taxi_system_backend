namespace Microsoft.Extensions.DependencyInjection;

using System.Text;

using FirebaseAdmin;

using Google.Apis.Auth.OAuth2;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Infrastructure.Auth;
using Taxi.Infrastructure.BackgroundJobs;
using Taxi.Infrastructure.Common;
using Taxi.Infrastructure.Data;
using Taxi.Infrastructure.Data.Interceptors;
using Taxi.Infrastructure.Identity;
using Taxi.Infrastructure.Incidents;
using Taxi.Infrastructure.Maps;
using Taxi.Infrastructure.Notifications;
using Taxi.Infrastructure.Outbox;
using Taxi.Infrastructure.Payments;
using Taxi.Infrastructure.RealTime;
using Taxi.Infrastructure.Services.Invoices;
using Taxi.Infrastructure.Settings;
using Taxi.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<InvoiceIssuerOptions>(configuration.GetSection(InvoiceIssuerOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

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
                ClockSkew = TimeSpan.Zero,
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
            options.Password.RequiredLength = 5;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 1;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<ITokenProvider, TokenProvider>();

        services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
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
        services.AddSingleton<IClientConfigProvider, ClientConfigProvider>();

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
        services.AddHostedService<OutboxDispatcherService>();
        services.AddHostedService<TripChatCleanupService>();

        return services;
    }
}
