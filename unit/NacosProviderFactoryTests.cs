using Microsoft.Extensions.Logging;
using Moq;
using Nacos.V2;
using Ocelot.Configuration;

namespace Ocelot.Discovery.Nacos.UnitTests;

[TestClass]
public class NacosProviderFactoryTests
{
    [TestMethod]
    public void CreateProvider_ShouldReturnNacosProvider_WhenConfigTypeIsNacos()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<Ocelot.Discovery.Nacos.Nacos>>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<Ocelot.Discovery.Nacos.Nacos>)))
            .Returns(loggerMock.Object);
        var nacosNamingServiceMock = new Mock<INacosNamingService>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(INacosNamingService)))
            .Returns(nacosNamingServiceMock.Object);

        var config = new ServiceProviderConfiguration(
            type: "nacos",
            scheme: "http",
            host: "localhost",
            port: 8848,
            token: null,
            configurationKey: "nacos",
            pollingInterval: 5000
        );

        var route = new DownstreamRoute(
            key: "testKey",
            upstreamPathTemplate: null,
            upstreamHeadersFindAndReplace: null,
            downstreamHeadersFindAndReplace: null,
            downstreamAddresses: null,
            serviceName: "testService",
            serviceNamespace: null,
            httpHandlerOptions: null,
            useServiceDiscovery: false,
            enableEndpointEndpointRateLimiting: false,
            qosOptions: null,
            downstreamScheme: null,
            requestIdKey: null,
            isCached: false,
            cacheOptions: null,
            loadBalancerOptions: null,
            rateLimitOptions: null,
            routeClaimsRequirement: null,
            claimsToQueries: null,
            claimsToHeaders: null,
            claimsToClaims: null,
            claimsToPath: null,
            isAuthenticated: false,
            isAuthorized: false,
            authenticationOptions: null,
            downstreamPathTemplate: null,
            loadBalancerKey: null,
            delegatingHandlers: null,
            addHeadersToDownstream: null,
            addHeadersToUpstream: null,
            dangerousAcceptAnyServerCertificateValidator: false,
            securityOptions: null,
            downstreamHttpMethod: null,
            downstreamHttpVersion: null,
            downstreamHttpVersionPolicy: default,
            upstreamHeaders: null,
            metadataOptions: null
        );

        // Act
        var provider = NacosProviderFactory.Get(serviceProviderMock.Object, config, route);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(Ocelot.Discovery.Nacos.Nacos));
    }

    [TestMethod]
    public void CreateProvider_ShouldThrowException_WhenNacosNamingServiceIsNull()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(INacosNamingService)))
            .Returns(null!);

        var config = new ServiceProviderConfiguration(
            type: "nacos",
            scheme: "http",
            host: "localhost",
            port: 8848,
            token: null,
            configurationKey: "nacos",
            pollingInterval: 5000
        );

        var route = new DownstreamRoute(
            key: "testKey",
            upstreamPathTemplate: null,
            upstreamHeadersFindAndReplace: null,
            downstreamHeadersFindAndReplace: null,
            downstreamAddresses: null,
            serviceName: "testService",
            serviceNamespace: null,
            httpHandlerOptions: null,
            useServiceDiscovery: false,
            enableEndpointEndpointRateLimiting: false,
            qosOptions: null,
            downstreamScheme: null,
            requestIdKey: null,
            isCached: false,
            cacheOptions: null,
            loadBalancerOptions: null,
            rateLimitOptions: null,
            routeClaimsRequirement: null,
            claimsToQueries: null,
            claimsToHeaders: null,
            claimsToClaims: null,
            claimsToPath: null,
            isAuthenticated: false,
            isAuthorized: false,
            authenticationOptions: null,
            downstreamPathTemplate: null,
            loadBalancerKey: null,
            delegatingHandlers: null,
            addHeadersToDownstream: null,
            addHeadersToUpstream: null,
            dangerousAcceptAnyServerCertificateValidator: false,
            securityOptions: null,
            downstreamHttpMethod: null,
            downstreamHttpVersion: null,
            downstreamHttpVersionPolicy: default,
            upstreamHeaders: null,
            metadataOptions: null
        );

        // Act, Assert
        Assert.ThrowsException<NullReferenceException>(() =>
            NacosProviderFactory.Get(serviceProviderMock.Object, config, route));
    }

    [TestMethod]
    public void CreateProvider_ShouldReturnNull_WhenLoggerIsNull()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<Ocelot.Discovery.Nacos.Nacos>)))
            .Returns(null!);
        var nacosNamingServiceMock = new Mock<INacosNamingService>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(INacosNamingService)))
            .Returns(nacosNamingServiceMock.Object);

        var config = new ServiceProviderConfiguration(
            type: "nacos",
            scheme: "http",
            host: "localhost",
            port: 8848,
            token: null,
            configurationKey: "nacos",
            pollingInterval: 5000
        );

        var route = new DownstreamRoute(
            key: "testKey",
            upstreamPathTemplate: null,
            upstreamHeadersFindAndReplace: null,
            downstreamHeadersFindAndReplace: null,
            downstreamAddresses: null,
            serviceName: "testService",
            serviceNamespace: null,
            httpHandlerOptions: null,
            useServiceDiscovery: false,
            enableEndpointEndpointRateLimiting: false,
            qosOptions: null,
            downstreamScheme: null,
            requestIdKey: null,
            isCached: false,
            cacheOptions: null,
            loadBalancerOptions: null,
            rateLimitOptions: null,
            routeClaimsRequirement: null,
            claimsToQueries: null,
            claimsToHeaders: null,
            claimsToClaims: null,
            claimsToPath: null,
            isAuthenticated: false,
            isAuthorized: false,
            authenticationOptions: null,
            downstreamPathTemplate: null,
            loadBalancerKey: null,
            delegatingHandlers: null,
            addHeadersToDownstream: null,
            addHeadersToUpstream: null,
            dangerousAcceptAnyServerCertificateValidator: false,
            securityOptions: null,
            downstreamHttpMethod: null,
            downstreamHttpVersion: null,
            downstreamHttpVersionPolicy: default,
            upstreamHeaders: null,
            metadataOptions: null
        );

        // Act
        var provider = NacosProviderFactory.Get(serviceProviderMock.Object, config, route);

        // Assert
        Assert.IsNull(provider);
    }

    [TestMethod]
    public void CreateProvider_ShouldReturnNull_WhenConfigTypeIsNotNacos()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<Ocelot.Discovery.Nacos.Nacos>>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<Ocelot.Discovery.Nacos.Nacos>)))
            .Returns(loggerMock.Object);
        var nacosNamingServiceMock = new Mock<INacosNamingService>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(INacosNamingService)))
            .Returns(nacosNamingServiceMock.Object);

        var config = new ServiceProviderConfiguration(
            type: "other",
            scheme: "http",
            host: "localhost",
            port: 8848,
            token: null,
            configurationKey: "nacos",
            pollingInterval: 5000
        );

        var route = new DownstreamRoute(
            key: "testKey",
            upstreamPathTemplate: null,
            upstreamHeadersFindAndReplace: null,
            downstreamHeadersFindAndReplace: null,
            downstreamAddresses: null,
            serviceName: "testService",
            serviceNamespace: null,
            httpHandlerOptions: null,
            useServiceDiscovery: false,
            enableEndpointEndpointRateLimiting: false,
            qosOptions: null,
            downstreamScheme: null,
            requestIdKey: null,
            isCached: false,
            cacheOptions: null,
            loadBalancerOptions: null,
            rateLimitOptions: null,
            routeClaimsRequirement: null,
            claimsToQueries: null,
            claimsToHeaders: null,
            claimsToClaims: null,
            claimsToPath: null,
            isAuthenticated: false,
            isAuthorized: false,
            authenticationOptions: null,
            downstreamPathTemplate: null,
            loadBalancerKey: null,
            delegatingHandlers: null,
            addHeadersToDownstream: null,
            addHeadersToUpstream: null,
            dangerousAcceptAnyServerCertificateValidator: false,
            securityOptions: null,
            downstreamHttpMethod: null,
            downstreamHttpVersion: null,
            downstreamHttpVersionPolicy: default,
            upstreamHeaders: null,
            metadataOptions: null
        );

        // Act
        var provider = NacosProviderFactory.Get(serviceProviderMock.Object, config, route);

        // Assert
        Assert.IsNull(provider);
    }
}