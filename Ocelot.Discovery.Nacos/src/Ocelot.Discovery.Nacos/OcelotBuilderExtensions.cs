using Ocelot.DependencyInjection;
using Nacos.AspNetCore.V2;

namespace Ocelot.Discovery.Nacos
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddNacos(this IOcelotBuilder builder, string section = "nacos")
        {
            builder.Services
                .AddNacosAspNet(builder.Configuration,section)
                .AddSingleton(NacosProviderFactory.Get)
                .AddSingleton(NacosMiddlewareConfigurationProvider.Get);
            return builder;
        }
    }
}
