using Microsoft.Extensions.Logging;
using Moq;
using Nacos.V2;
using Nacos.V2.Exceptions;
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
    
    [TestMethod]
    public async Task GetAsync_ShouldReturnEmptyList_WhenExceptionOccurs()
    {
        // Arrange
        var testLogger = new TestLogger<Nacos>();
        _nacos = new Nacos("testService", _mockNacosNamingService.Object, testLogger);
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService"))
            .ThrowsAsync(new NacosException("Test exception"));

        // Act
        var result = await _nacos.GetAsync();

        // Assert
        Assert.AreEqual(0, result.Count);
        var logEntry = testLogger.LogEntries.FirstOrDefault(e => 
            e.LogLevel == LogLevel.Error && 
            e.Message.Contains("An exception occurred while fetching instances for service testService from Nacos."));
        Assert.IsNotNull(logEntry);
        Assert.IsInstanceOfType(logEntry.Exception, typeof(NacosException));
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
