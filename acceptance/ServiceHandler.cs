﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Ocelot.Discovery.Nacos.AcceptanceTests;

public class ServiceHandler : IDisposable
{
    private IWebHost _builder;

    public void GivenThereIsAServiceRunningOn(string baseUrl, RequestDelegate handler)
    {
        _builder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.Run(handler);
            })
            .Build();

        _builder.Start();
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, RequestDelegate handler)
    {
        _builder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(handler);
            })
            .Build();

        _builder.Start();
    }

    public void GivenThereIsAServiceRunningOnWithKestrelOptions(string baseUrl, string basePath, Action<KestrelServerOptions> options, RequestDelegate handler)
    {
        _builder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .ConfigureKestrel(options ?? WithDefaultKestrelServerOptions) // !
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(handler);
            })
            .Build();

        _builder.Start();
    }

    internal void WithDefaultKestrelServerOptions(KestrelServerOptions options)
    {
    }

    public void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, string fileName, string password, int port, RequestDelegate handler)
    {
        _builder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, port, listenOptions =>
                {
                    listenOptions.UseHttps(fileName, password);
                });
            })
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(handler);
            })
            .Build();

        _builder.Start();
    }

    public async Task StartFakeDownstreamService(string url, Func<HttpContext, Func<Task>, Task> middleware)
    {
        _builder = TestHostBuilder.Create()
            .ConfigureServices(s => { }).UseKestrel()
            .UseUrls(url)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            })
            .Configure(app =>
            {
                app.UseWebSockets();
                app.Use(middleware);
            })
            .UseIISIntegration()
            .Build();

        await _builder.StartAsync();
    }

    public void Dispose()
    {
        _builder?.Dispose();
        GC.SuppressFinalize(this);
    }
}
