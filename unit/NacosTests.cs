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
        var logger = new Mock<IOcelotLogger>();
        logger.Setup(l => l.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()))
            .Callback<Func<string>, Exception>(AssertMessage)
            .Verifiable($"{nameof(IOcelotLogger)}.{nameof(IOcelotLogger.LogError)}() was not called.");
        var factory = new Mock<IOcelotLoggerFactory>();
        factory.Setup(f => f.CreateLogger<Nacos>()).Returns(logger.Object);

        _nacos = new Nacos("testService", _mockNacosNamingService.Object, factory.Object);
        var ex = new NacosException("Test exception");
        _mockNacosNamingService?.Setup(x => x.GetAllInstances("testService"))
            .ThrowsAsync(ex);

        // Act
        var result = await _nacos.GetAsync();

        // Assert
        Assert.AreEqual(0, result.Count);
        logger.Verify(l => l.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()),
            Times.Once);
        static void AssertMessage(Func<string> messageFactory, Exception exception)
        {
            Assert.IsNotNull(messageFactory);
            Assert.IsNotNull(exception);
            string message = messageFactory.Invoke();
            Assert.AreEqual("Nacos discovery: An exception occurred while fetching instances for service:testService from Nacos.", message);
            Assert.IsInstanceOfType<NacosException>(exception);
        }
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
