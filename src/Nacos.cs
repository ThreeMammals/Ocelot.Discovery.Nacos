using Service = Ocelot.Values.Service;

namespace Ocelot.Discovery.Nacos;

public class Nacos : IServiceDiscoveryProvider
{
    private readonly INacosNamingService _client;
    private readonly string _serviceName;
    private readonly ILogger<Nacos> _logger;

    public Nacos(string serviceName, INacosNamingService client, ILogger<Nacos> logger)
    {
        _client = client;
        _serviceName = serviceName;
        _logger = logger;
    }

    public async Task<List<Service>> GetAsync()
    {
        try
        {
            var instances = await _client.GetAllInstances(_serviceName)
                .ConfigureAwait(false);

            return instances?
                .Where(i => i.Healthy && i.Enabled && i.Weight > 0) // Filter out unhealthy instances
                .Select(TransformInstance)
                .ToList() ?? new();
        }
        catch (NacosException ex)
        {
            _logger.LogError(ex, $"An exception occurred while fetching instances for service {_serviceName} from Nacos.");
            return new();
        }
    }

    private Service TransformInstance(Instance instance)
    {
        var metadata = instance.Metadata ?? new();

        return new Service(
            id: instance.InstanceId,
            hostAndPort: new(instance.Ip, instance.Port),
            name: instance.ServiceName,
            version: metadata.GetValueOrDefault("version", "default"),
            tags: Nacos.ProcessMetadataTags(metadata)
        );
    }

    private static List<string> ProcessMetadataTags(IDictionary<string, string> metadata) => metadata
        .Where(kv => !_reservedKeys.Contains(kv.Key))
        .Select(FormatTag)
        .ToList();

    private static string FormatTag(KeyValuePair<string, string> kv)
        => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}";

    private static readonly string[] _reservedKeys = { "version", "group", "cluster", "namespace", "weight" };
}
