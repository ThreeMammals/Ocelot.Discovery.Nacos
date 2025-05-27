using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.DependencyInjection;

namespace Ocelot.Discovery.Nacos.UnitTests;

[TestClass]
public class OcelotBuilderExtensionsTests
{
    [TestMethod]
    public void AddNacos_ShouldRegisterNacosServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new Mock<IConfiguration>();
        var builder = new OcelotBuilder(services, configuration.Object);

        // Act
        builder.Services.AddOcelot().AddNacos();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.IsNotNull(serviceProvider.GetService(NacosProviderFactory.Get.GetType()));
        Assert.IsNotNull(serviceProvider.GetService(NacosMiddlewareConfigurationProvider.Get.GetType()));
    }
}
