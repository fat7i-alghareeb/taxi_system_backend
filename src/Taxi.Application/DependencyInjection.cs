namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using FluentValidation;
using Taxi.Application.Common.Behaviours;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Services;
using Taxi.Application.Features.Payments.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IRefundLifecycleService, RefundLifecycleService>();
        services.AddScoped<IFeeSettlementService, FeeSettlementService>();
        services.AddScoped<IAuthSessionFactory, AuthSessionFactory>();

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

