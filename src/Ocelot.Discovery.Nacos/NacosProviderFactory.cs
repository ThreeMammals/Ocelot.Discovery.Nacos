using Ocelot.ServiceDiscovery;

namespace Ocelot.Discovery.Nacos;

public static class NacosProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string Nacos = nameof(Discovery.Nacos.Nacos);

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var client = provider.GetService<INacosNamingService>();
        if (client == null)
        {
            throw new NullReferenceException($"Cannot get an {nameof(INacosNamingService)} service during {nameof(CreateProvider)} operation to instantiate the {nameof(Nacos)} provider!");
        }

        return Nacos.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
            ? new Nacos(route.ServiceName, client)
            : null;
    }
}
