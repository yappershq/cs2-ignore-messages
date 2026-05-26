using IgnoreMessages.Configuration;
using IgnoreMessages.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace IgnoreMessages;

internal static class ModuleDependencyInjection
{
    internal static IServiceCollection AddModules(this IServiceCollection services)
    {
        // Configuration (constructs ConVars on instantiation — not an IModule)
        services.AddSingleton<IIgnoreMessagesConfig, IgnoreMessagesConfig>();

        // Net message filter
        services.AddSingleton<NetMessageFilterModule>();
        services.AddSingleton<IModule>(sp => sp.GetRequiredService<NetMessageFilterModule>());

        return services;
    }
}
