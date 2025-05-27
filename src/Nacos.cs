using Ocelot.Logging;
using Service = Ocelot.Values.Service;

namespace Ocelot.Discovery.Nacos;

public class Nacos : IServiceDiscoveryProvider
{
    private readonly INacosNamingService _client;
    private readonly string _serviceName;
    private readonly IOcelotLogger _logger;

    public Nacos(string serviceName, INacosNamingService client, IOcelotLoggerFactory factory)
    {
        _client = client;
        _serviceName = serviceName;
        _logger = factory.CreateLogger<Nacos>();
        ;
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
            _logger.LogError(
                () => $"{nameof(Nacos)} discovery: An exception occurred while fetching instances for service:{_serviceName} from Nacos.",
                ex);
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
            tags: ProcessMetadataTags(metadata)
        );
    }

    private static List<string> ProcessMetadataTags(IDictionary<string, string> metadata) => metadata
        .Where(kv => !ReservedKeys.Contains(kv.Key))
        .Select(FormatTag)
        .ToList();

    private static string FormatTag(KeyValuePair<string, string> kv)
        => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}";

    /// <summary>
    /// Reserved keys that should not be included in the metadata tags.
    /// These keys are used internally by the Nacos service discovery provider
    /// and should not be exposed as part of the service metadata tags.
    /// Version key used to specify the version of the service.
    /// Group key used to specify the group of the service.
    /// Cluster key used to specify the cluster of the service.
    /// Namespace key used to specify the namespace of the service.
    /// Weight key used to specify the weight of the service instance.
    /// </summary>
    public static readonly string[] ReservedKeys = { "version", "group", "cluster", "namespace", "weight" };
}