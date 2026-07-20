namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using FluentValidation;
using Taxi.Application.Common.Behaviours;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Services;
using Taxi.Application.Features.Payments.Services;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Wallet.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IRefundLifecycleService, RefundLifecycleService>();
        services.AddScoped<ITripRefundSplitter, TripRefundSplitter>();
        services.AddScoped<IFeeSettlementService, FeeSettlementService>();
        services.AddScoped<IFareAdjustmentSettlementService, FareAdjustmentSettlementService>();
        services.AddScoped<ITripRequoteService, TripRequoteService>();
        services.AddScoped<ITripEditApplier, TripEditApplier>();
        services.AddScoped<ITripAdminEditNotifier, TripAdminEditNotifier>();
        services.AddScoped<IAuthSessionFactory, AuthSessionFactory>();
        services.AddScoped<IWalletDebtGuard, WalletDebtGuard>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });

        return services;
    }
}

