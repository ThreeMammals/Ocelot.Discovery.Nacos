using Microsoft.Extensions.Logging;
using Moq;
using Nacos.V2;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;

namespace Ocelot.Discovery.Nacos.UnitTests;

[TestClass]
public class NacosProviderFactoryTests
{
    [TestMethod]
    public void CreateProvider_ShouldReturnNacosProvider_WhenConfigTypeIsNacos()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<IOcelotLoggerFactory>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOcelotLoggerFactory)))
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
        var route = new DownstreamRouteBuilder()
            .WithKey("testKey")
            .WithServiceName("testService")
            .Build();

        // Act
        var provider = NacosProviderFactory.Get(serviceProviderMock.Object, config, route);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(Nacos));
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
        var route = new DownstreamRouteBuilder()
            .WithKey("testKey")
            .WithServiceName("testService")
            .Build();

        // Act, Assert
        Assert.ThrowsException<NullReferenceException>(() =>
            NacosProviderFactory.Get(serviceProviderMock.Object, config, route));
    }

    [TestMethod]
    public void CreateProvider_ShouldReturnNull_WhenLoggerIsNull()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<Nacos>)))
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
        var route = new DownstreamRouteBuilder()
            .WithKey("testKey")
            .WithServiceName("testService")
            .Build();

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
        var loggerMock = new Mock<ILogger<Nacos>>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<Nacos>)))
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
        var route = new DownstreamRouteBuilder()
            .WithKey("testKey")
            .WithServiceName("testService")
            .Build();

        // Act
        var provider = NacosProviderFactory.Get(serviceProviderMock.Object, config, route);

        // Assert
        Assert.IsNull(provider);
    }
}
