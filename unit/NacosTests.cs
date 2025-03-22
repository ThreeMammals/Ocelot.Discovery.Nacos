using Microsoft.Extensions.Logging;
using Moq;
using Nacos.V2;
using Nacos.V2.Exceptions;
using Nacos.V2.Naming.Dtos;
using Ocelot.Logging;

namespace Ocelot.Discovery.Nacos.UnitTests;

[TestClass]
public class NacosTests
{
    private readonly Mock<INacosNamingService> _mockNacosNamingService;
    private readonly Mock<IOcelotLoggerFactory> _mockLogger;
    private Nacos _nacos;

    public NacosTests()
    {
        _mockNacosNamingService = new Mock<INacosNamingService>();
        _mockLogger = new Mock<IOcelotLoggerFactory>();
        _nacos = new Nacos("testService", _mockNacosNamingService.Object, _mockLogger.Object);
    }

    [TestInitialize]
    public void Setup()
    {
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
    
    [TestMethod]
    public async Task GetAsync_ShouldReturnEmptyList_WhenExceptionOccurs()
    {
        // Arrange
        var testLogger = new Mock<IOcelotLogger>();
        var testLoggerFactory = new Mock<IOcelotLoggerFactory>();
        testLoggerFactory.Setup(f => f.CreateLogger<Nacos>()).Returns(testLogger.Object);
        _nacos = new Nacos("testService", _mockNacosNamingService.Object, testLoggerFactory.Object);
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService"))
            .ThrowsAsync(new NacosException("Test exception"));

        // Act
        var result = await _nacos.GetAsync();

        // Assert
        Assert.AreEqual(0, result.Count);
        testLogger.Verify(
            l => l.LogError(It.Is<string>(msg => msg.Contains("An exception occurred while fetching instances for service testService from Nacos.")), 
                It.IsAny<NacosException>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task GetAsync_ShouldReturnServices_WithCorrectMetadataTags()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new()
            {
                InstanceId = "1", Ip = "127.0.0.1", Port = 80, ServiceName = "testService", Healthy = true, Enabled = true, Weight = 1,
                Metadata = new Dictionary<string, string> { { "version", "1.0" }, { "customTag", "customValue" } }
            }
        };
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService")).ReturnsAsync(instances);

        // Act
        var result = await _nacos?.GetAsync()!;

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].Id);
        Assert.AreEqual("1.0", result[0].Version);
        Assert.IsTrue(result[0].Tags.Contains("customTag=customValue"));
        Assert.IsFalse(result[0].Tags.Contains("version=1.0"));
    }
}
