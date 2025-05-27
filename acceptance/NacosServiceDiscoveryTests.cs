using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using Shouldly;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Ocelot.Discovery.Nacos.AcceptanceTests;
using LoadBalancer;

public sealed class NacosServiceDiscoveryTests : ConcurrentSteps, IDisposable
{
    private readonly string _kubernetesUrl;
    private readonly ServiceHandler _kubernetesHandler;
    private string _receivedToken;
    //private readonly Action<KubeClientOptions> _kubeClientOptionsConfigure;

    public NacosServiceDiscoveryTests()
    {
        _kubernetesUrl = DownstreamUrl(PortFinder.GetRandomPort());
        //_kubeClientOptionsConfigure = opts =>
        //{
        //    opts.ApiEndPoint = new Uri(_kubernetesUrl);
        //    opts.AccessToken = "txpc696iUhbVoudg164r93CxDTrKRVWG";
        //    opts.AuthStrategy = KubeAuthStrategy.BearerToken;
        //    opts.AllowInsecure = true;
        //};
        _kubernetesHandler = new();
    }

    public override void Dispose()
    {
        _kubernetesHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public async Task ShouldReturnServicesFromK8s()
    {
        const string namespaces = nameof(NacosServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s);
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, route);
        var downstreamResponse = serviceName;

        GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse);
        GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetes);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe($"1:{downstreamResponse}");
        ThenAllServicesShouldHaveBeenCalledTimes(1);
        ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG");
    }

    [Theory]
    [Trait("Feat", "1967")]
    [InlineData("", HttpStatusCode.BadGateway)]
    [InlineData("http", HttpStatusCode.OK)]
    public async Task ShouldReturnServicesByPortNameAsDownstreamScheme(string downstreamScheme, HttpStatusCode statusCode)
    {
        const string serviceName = "example-web";
        const string namespaces = "default";
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);

        // Ports[0] -> port(https, 443)
        // Ports[1] -> port(http, not 80)
        subsetV1.Ports.Insert(0, new()
        {
            Name = "https", // This service instance is offline -> BadGateway
            Port = 443,
        });
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        route.DownstreamPathTemplate = "/{url}";
        route.DownstreamScheme = downstreamScheme; // !!! Warning !!! Select port by name as scheme
        route.UpstreamPathTemplate = "/api/example/{url}";
        route.ServiceName = serviceName; // "example-web"
        var configuration = GivenKubeConfiguration(namespaces, route);

        GivenServiceInstanceIsRunning(downstreamUrl, nameof(ShouldReturnServicesByPortNameAsDownstreamScheme));
        GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetes);
        await WhenIGetUrlOnTheApiGateway("/api/example/1");
        ThenTheStatusCodeShouldBe(statusCode);
        ThenTheResponseBodyShouldBe(downstreamScheme == "http"
                            ? "1:" + nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)
                            : string.Empty);
        ThenAllServicesShouldHaveBeenCalledTimes(downstreamScheme == "http" ? 1 : 0);
        ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG");
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(1, 30)]
    [InlineData(2, 50)]
    [InlineData(3, 50)]
    [InlineData(4, 50)]
    [InlineData(5, 50)]
    [InlineData(6, 99)]
    [InlineData(7, 99)]
    [InlineData(8, 99)]
    [InlineData(9, 999)]
    [InlineData(10, 999)]
    public void ShouldHighlyLoadOnStableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests)
    {
        const int ZeroGeneration = 0;
        var (endpoints, servicePorts) = ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(totalServices);
        GivenThereIsAFakeKubernetesProvider(endpoints); // stable, services will not be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(totalRequests, ZeroGeneration);

        int bottom = totalRequests / totalServices,
            top = totalRequests - (bottom * totalServices) + bottom;
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top);
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(5, 50, 1)]
    [InlineData(5, 50, 2)]
    [InlineData(5, 50, 3)]
    [InlineData(5, 50, 4)]
    public void ShouldHighlyLoadOnUnstableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests, int k8sGeneration)
    {
        int failPerThreads = (totalRequests / k8sGeneration) - 1; // k8sGeneration means number of offline services
        var (endpoints, servicePorts) = ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(totalServices);
        GivenThereIsAFakeKubernetesProvider(endpoints, false, k8sGeneration, failPerThreads); // false means unstable, k8sGeneration services will be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(totalRequests, k8sGeneration);

        ThenAllServicesCalledOptimisticAmountOfTimes(_roundRobinAnalyzer); // with unstable checkings
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    [Fact]
    [Trait("Feat", "2256")]
    public async Task ShouldReturnServicesFromK8s_AddKubernetesWithNullConfigureOptions()
    {
        const string namespaces = nameof(NacosServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s_AddKubernetesWithNullConfigureOptions);
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, route, "txpc696iUhbVoudg164r93CxDTrKRVWG");
        var downstreamResponse = serviceName;
        GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse);
        GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(AddKubernetesWithNullConfigureOptions);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe($"1:{downstreamResponse}");
        ThenAllServicesShouldHaveBeenCalledTimes(1);
        ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG");
    }

    private void AddKubernetesWithNullConfigureOptions(IServiceCollection services)
        => services.AddOcelot(); //.AddKubernetes(configureOptions: null);

    private (EndpointsV1 Endpoints, int[] ServicePorts) ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(
        int totalServices,
        [CallerMemberName] string serviceName = nameof(ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer))
    {
        const string namespaces = nameof(NacosServiceDiscoveryTests);
        var servicePorts = PortFinder.GetPorts(totalServices);
        var downstreamUrls = servicePorts
            .Select(port => LoopbackLocalhostUrl(port, Array.IndexOf(servicePorts, port)))
            .ToArray(); // based on localhost aka loopback network interface
        var downstreams = downstreamUrls.Select(url => new Uri(url))
            .ToList();
        var downstreamResponses = downstreams
            .Select(ds => $"{serviceName}:{ds.Host}:{ds.Port}")
            .ToArray();
        var subset = new EndpointSubsetV1();
        downstreams.ForEach(ds => GivenSubsetAddress(ds, subset));
        var endpoints = GivenEndpoints(subset, serviceName); // totalServices service instances with different ports
        var route = GivenRouteWithServiceName(namespaces, serviceName, nameof(RoundRobinAnalyzer)); // !!!
        var configuration = GivenKubeConfiguration(namespaces, route);
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, downstreamResponses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetesAndRoundRobin);
        return (endpoints, servicePorts);
    }

    private void HighlyLoadOnKubeProviderAndRoundRobinBalancer(int totalRequests, int k8sGenerationNo)
    {
        // Act
        WhenIGetUrlOnTheApiGatewayConcurrently("/", totalRequests); // load by X parallel requests

        // Assert
        _k8sCounter.ShouldBeGreaterThanOrEqualTo(totalRequests); // integration endpoint called times
        _k8sServiceGeneration.ShouldBe(k8sGenerationNo);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);
        _roundRobinAnalyzer.ShouldNotBeNull().Analyze();
        _roundRobinAnalyzer.Events.Count.ShouldBe(totalRequests);
        _roundRobinAnalyzer.HasManyServiceGenerations(k8sGenerationNo).ShouldBeTrue();
    }

    private void ThenTheTokenIs(string token)
    {
        _receivedToken.ShouldBe(token);
    }

    private EndpointsV1 GivenEndpoints(EndpointSubsetV1 subset, [CallerMemberName] string serviceName = "")
    {
        var e = new EndpointsV1()
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new()
            {
                Name = serviceName,
                Namespace = nameof(NacosServiceDiscoveryTests),
            },
        };
        e.Subsets.Add(subset);
        return e;
    }

    private static EndpointSubsetV1 GivenSubsetAddress(Uri downstream, EndpointSubsetV1 subset = null)
    {
        subset ??= new();
        subset.Addresses.Add(new()
        {
            Ip = Dns.GetHostAddresses(downstream.Host).Select(x => x.ToString()).First(a => a.Contains('.')), // 127.0.0.1
            Hostname = downstream.Host,
        });
        subset.Ports.Add(new()
        {
            Name = downstream.Scheme,
            Port = downstream.Port,
        });
        return subset;
    }

    private FileRoute GivenRouteWithServiceName(string serviceNamespace,
        [CallerMemberName] string serviceName = null,
        string loadBalancerType = nameof(LeastConnection)) => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamScheme = null, // the scheme should not be defined in service discovery scenarios by default, only ServiceName
            UpstreamPathTemplate = "/",
            UpstreamHttpMethod = new() { HttpMethods.Get },
            ServiceName = serviceName, // !!!
            ServiceNamespace = serviceNamespace,
            LoadBalancerOptions = new() { Type = loadBalancerType },
        };

    private FileConfiguration GivenKubeConfiguration(string serviceNamespace, FileRoute route, string token = null)
    {
        var u = new Uri(_kubernetesUrl);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = u.Scheme,
            Host = u.Host,
            Port = u.Port,
            Type = "", //nameof(Kube),
            PollingInterval = 0,
            Namespace = serviceNamespace,
            Token = token ?? "Test",
        };
        return configuration;
    }

    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints,
        [CallerMemberName] string serviceName = nameof(NacosServiceDiscoveryTests), string namespaces = nameof(NacosServiceDiscoveryTests))
        => GivenThereIsAFakeKubernetesProvider(endpoints, true, 0, 0, serviceName, namespaces);

    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints, bool isStable, int offlineServicesNo, int offlinePerThreads,
        [CallerMemberName] string serviceName = nameof(NacosServiceDiscoveryTests), string namespaces = nameof(NacosServiceDiscoveryTests))
    {
        _k8sCounter = 0;
        _kubernetesHandler.GivenThereIsAServiceRunningOn(_kubernetesUrl, async context =>
        {
            await Task.Delay(Random.Shared.Next(1, 10)); // emulate integration delay up to 10 milliseconds
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                string json;
                lock (K8sCounterLocker)
                {
                    _k8sCounter++;
                    var subset = endpoints.Subsets[0];

                    // Each offlinePerThreads-th request to integrated K8s endpoint should fail
                    if (!isStable && _k8sCounter % offlinePerThreads == 0 && _k8sCounter >= offlinePerThreads)
                    {
                        while (offlineServicesNo-- > 0)
                        {
                            int index = subset.Addresses.Count - 1; // Random.Shared.Next(0, subset.Addresses.Count - 1);
                            subset.Addresses.RemoveAt(index);
                            subset.Ports.RemoveAt(index);
                        }

                        _k8sServiceGeneration++;
                    }

                    endpoints.Metadata.Generation = _k8sServiceGeneration;
                    json = JsonConvert.SerializeObject(endpoints/*,KubeResourceClient.SerializerSettings*/);
                }

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    _receivedToken = values.First();
                }

                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
        });
    }

    private static ServiceDescriptor GetValidateScopesDescriptor()
        => ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(
            new DefaultServiceProviderFactory(new() { ValidateScopes = true }));
    private IOcelotBuilder AddKubernetes(IServiceCollection services) => services
        .Replace(GetValidateScopesDescriptor())
        .AddOcelot();//.AddKubernetes(_kubeClientOptionsConfigure);

    private void WithKubernetes(IServiceCollection services) => AddKubernetes(services);
    private void WithKubernetesAndRoundRobin(IServiceCollection services) => AddKubernetes(services)
        .AddCustomLoadBalancer<RoundRobinAnalyzer>(GetRoundRobinAnalyzer);
        //.Services.RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();

    private int _k8sCounter, _k8sServiceGeneration;
    private static readonly object K8sCounterLocker = new();
    private RoundRobinAnalyzer _roundRobinAnalyzer;
    private RoundRobinAnalyzer GetRoundRobinAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
    {
        lock (K8sCounterLocker)
        {
            return _roundRobinAnalyzer ??= new RoundRobinAnalyzerCreator().Create(route, provider)?.Data as RoundRobinAnalyzer; //??= new RoundRobinAnalyzer(provider.GetAsync, route.ServiceName);
        }
    }
}

internal class FakeKubeServiceCreator //: KubeServiceCreator
{
    public FakeKubeServiceCreator(IOcelotLoggerFactory factory) { } //: base(factory) { }

    protected /*override*/ ServiceHostAndPort GetServiceHostAndPort(/*KubeRegistryConfiguration configuration,*/ EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var ports = subset.Ports;
        var index = subset.Addresses.IndexOf(address);
        var portV1 = ports[index];
        //Logger.LogDebug(() => $"K8s service with key '{configuration.KeyOfServiceInK8s}' and address {address.Ip}; Detected port is {portV1.Name}:{portV1.Port}. Total {ports.Count} ports of [{string.Join(',', ports.Select(p => p.Name))}].");
        return new ServiceHostAndPort(address.Ip, (int)portV1.Port, portV1.Name);
    }

    protected /*override*/ IEnumerable<string> GetServiceTags(/*KubeRegistryConfiguration configuration,*/ EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var tags = new List<string>(); //base.GetServiceTags(configuration, endpoint, subset, address).ToList();
        long gen = endpoint.Metadata.Generation ?? 0L;
        tags.Add($"{nameof(endpoint.Metadata.Generation)}:{gen}");
        return tags;
    }
}

public class EndpointsV1 //: KubeResourceV1
{
    public List<EndpointSubsetV1> Subsets { get; } = new List<EndpointSubsetV1>();
    public bool ShouldSerializeSubsets() => Subsets.Count > 0;

    [JsonProperty("metadata")]
    public ObjectMetaV1 Metadata { get; set; }

    [JsonProperty("kind")]
    public string Kind { get; set; }

    [JsonProperty("apiVersion")]
    public string ApiVersion { get; set; }
}

public class EndpointSubsetV1
{
    [JsonProperty("addresses", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public List<EndpointAddressV1> Addresses { get; } = new List<EndpointAddressV1>();


    [JsonProperty("notReadyAddresses", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public List<EndpointAddressV1> NotReadyAddresses { get; } = new List<EndpointAddressV1>();


    [JsonProperty("ports", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public List<EndpointPortV1> Ports { get; } = new List<EndpointPortV1>();


    public bool ShouldSerializeAddresses()
    {
        return Addresses.Count > 0;
    }

    public bool ShouldSerializeNotReadyAddresses()
    {
        return NotReadyAddresses.Count > 0;
    }

    public bool ShouldSerializePorts()
    {
        return Ports.Count > 0;
    }
}

public class EndpointAddressV1
{
    [JsonProperty("hostname", NullValueHandling = NullValueHandling.Ignore)]
    public string Hostname { get; set; }

    [JsonProperty("nodeName", NullValueHandling = NullValueHandling.Ignore)]
    public string NodeName { get; set; }

    //[JsonProperty("targetRef", NullValueHandling = NullValueHandling.Ignore)]
    //public ObjectReferenceV1 TargetRef { get; set; }

    [JsonProperty("ip", NullValueHandling = NullValueHandling.Include)]
    public string Ip { get; set; }
}

public class EndpointPortV1
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("appProtocol", NullValueHandling = NullValueHandling.Ignore)]
    public string AppProtocol { get; set; }

    [JsonProperty("protocol", NullValueHandling = NullValueHandling.Ignore)]
    public string Protocol { get; set; }

    [JsonProperty("port", NullValueHandling = NullValueHandling.Ignore)]
    public int? Port { get; set; }
}

public class ObjectMetaV1
{
    [JsonExtensionData]
    private readonly Dictionary<string, JToken> _extensionData = new Dictionary<string, JToken>();

    [JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
    public string Uid { get; set; }

    [JsonProperty("generateName", NullValueHandling = NullValueHandling.Ignore)]
    public string GenerateName { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("namespace", NullValueHandling = NullValueHandling.Ignore)]
    public string Namespace { get; set; }

    [JsonProperty("selfLink", NullValueHandling = NullValueHandling.Ignore)]
    public string SelfLink { get; set; }

    [JsonProperty("generation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Generation { get; set; }

    [JsonProperty("resourceVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string ResourceVersion { get; set; }

    [JsonProperty("creationTimestamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? CreationTimestamp { get; set; }

    [JsonProperty("deletionTimestamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? DeletionTimestamp { get; set; }

    [JsonProperty("annotations", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public Dictionary<string, string> Annotations { get; } = new Dictionary<string, string>();


    [JsonProperty("deletionGracePeriodSeconds", NullValueHandling = NullValueHandling.Ignore)]
    public long? DeletionGracePeriodSeconds { get; set; }

    [JsonProperty("finalizers", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public List<string> Finalizers { get; } = new List<string>();


    [JsonProperty("labels", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();


    //[JsonProperty("managedFields", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    //public List<ManagedFieldsEntryV1> ManagedFields { get; } = new List<ManagedFieldsEntryV1>();


    //[JsonProperty("ownerReferences", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
    //public List<OwnerReferenceV1> OwnerReferences { get; } = new List<OwnerReferenceV1>();


    [JsonIgnore]
    public IDictionary<string, JToken> ExtensionData => _extensionData;

    public bool ShouldSerializeAnnotations()
    {
        return Annotations.Count > 0;
    }

    public bool ShouldSerializeFinalizers()
    {
        return Finalizers.Count > 0;
    }

    public bool ShouldSerializeLabels()
    {
        return Labels.Count > 0;
    }

    //public bool ShouldSerializeManagedFields()
    //{
    //    return ManagedFields.Count > 0;
    //}

    //public bool ShouldSerializeOwnerReferences()
    //{
    //    return OwnerReferences.Count > 0;
    //}
}
