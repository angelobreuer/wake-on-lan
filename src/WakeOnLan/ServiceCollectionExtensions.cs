namespace WakeOnLan;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWakeOnLan(this IServiceCollection services)
    {
        services.TryAddSingleton<ISystemClock, SystemClock>();
        services.TryAddSingleton<IWolClientFactory, WolClientFactory>();

        return services;
    }
}
