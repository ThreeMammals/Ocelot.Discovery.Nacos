using Microsoft.AspNetCore.Builder;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;

namespace Ocelot.Discovery.Nacos;

public class NacosMiddlewareConfigurationProvider
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = GetInternal;

    private static Task GetInternal(IApplicationBuilder builder)
    {
        var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
        var log = builder.ApplicationServices.GetService<ILogger<NacosMiddlewareConfigurationProvider>>();
        var config = internalConfigRepo?.Get();

        if (config != null && UsingNacosServiceDiscoveryProvider(config.Data))
        {
            log?.LogInformation("Using Nacos service discovery provider.");
        }

        return Task.CompletedTask;
    }

    private static bool UsingNacosServiceDiscoveryProvider(IInternalConfiguration configuration)
        => nameof(Nacos).Equals(configuration?.ServiceProviderConfiguration?.Type, StringComparison.OrdinalIgnoreCase);
}
