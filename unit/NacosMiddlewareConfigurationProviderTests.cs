using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Discovery.Nacos.UnitTests
{
    [TestClass]
    public class NacosMiddlewareConfigurationProviderTests
    {
        [TestMethod]
        public void ShouldNotBuild()
        {
            var configRepo = new Mock<IInternalConfigurationRepository>();
            configRepo.Setup(x => x.Get())
                .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration(null, null, null, null, null,
                    null, null, null,null,null)));
            var services = new ServiceCollection();
            services.AddSingleton<IInternalConfigurationRepository>(configRepo.Object);
            var sp = services.BuildServiceProvider();
            var provider = NacosMiddlewareConfigurationProvider.Get(new ApplicationBuilder(sp));
            Assert.IsInstanceOfType(provider, typeof(Task));
        }

        [TestMethod]
        public void ShouldBuild()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().WithType("nacos").Build();
            var configRepo = new Mock<IInternalConfigurationRepository>();
            configRepo.Setup(x => x.Get())
                .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration
                (null, null,serviceProviderConfig, null, null, null, null, null, null, null)));
            var services = new ServiceCollection();
            services.AddSingleton<IInternalConfigurationRepository>(configRepo.Object);
            var sp = services.BuildServiceProvider();
            var provider = NacosMiddlewareConfigurationProvider.Get(new ApplicationBuilder(sp));
            Assert.IsInstanceOfType(provider, typeof(Task));
        }
    }
}