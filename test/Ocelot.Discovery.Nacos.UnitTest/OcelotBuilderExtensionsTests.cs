using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ocelot.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Ocelot.Discovery.Nacos;
using Nacos.AspNetCore.V2;

namespace Ocelot.Discovery.Nacos.Tests
{
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
}