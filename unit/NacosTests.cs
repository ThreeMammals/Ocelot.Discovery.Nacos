using Microsoft.Extensions.Logging;
using Moq;
using Nacos.V2;
using Nacos.V2.Naming.Dtos;

namespace Ocelot.Discovery.Nacos.UnitTests;

[TestClass]
public class NacosTests
{
    private Mock<INacosNamingService>? _mockNacosNamingService;
    private Nacos? _nacos;
    private Mock<ILogger<Nacos>>? _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockNacosNamingService = new Mock<INacosNamingService>();
        _mockLogger = new Mock<ILogger<Nacos>>();
        _nacos = new Nacos("testService", _mockNacosNamingService.Object,_mockLogger.Object);
    }

    [TestMethod]
    public async Task GetAsync_ShouldReturnServices_WhenInstancesAreHealthy()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new() { InstanceId = "1", Ip = "127.0.0.1", Port = 80, ServiceName = "testService", Healthy = true, Enabled = true, Weight = 1 },
            new() { InstanceId = "2", Ip = "127.0.0.2", Port = 81, ServiceName = "testService", Healthy = true, Enabled = true, Weight = 1 }
        };
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService")).ReturnsAsync(instances);

        // Act
        var result = await _nacos?.GetAsync()!;

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("1", result[0].Id);
        Assert.AreEqual("2", result[1].Id);
    }

    [TestMethod]
    public async Task GetAsync_ShouldReturnEmptyList_WhenNoInstancesAreHealthy()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new() { InstanceId = "1", Ip = "127.0.0.1", Port = 80, ServiceName = "testService", Healthy = false, Enabled = true, Weight = 1 },
            new() { InstanceId = "2", Ip = "127.0.0.2", Port = 81, ServiceName = "testService", Healthy = false, Enabled = true, Weight = 1 }
        };
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService")).ReturnsAsync(instances);

        // Act
        var result = await _nacos?.GetAsync()!;

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}
