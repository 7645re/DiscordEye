using System.Collections.Concurrent;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.FileManagers;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using Moq;

namespace DiscordEye.ProxyDistributor.Tests;

public class ProxyReservationServiceTests
{
    private readonly Mock<KeyedLockService> _lockerMock;
    private readonly Mock<IProxyStateFileManager> _fileManagerMock;
    private readonly ProxyReservationService _service;
    private readonly Proxy[] _testProxies;

    public ProxyReservationServiceTests()
    {
        _lockerMock = new Mock<KeyedLockService>();
        _fileManagerMock = new Mock<IProxyStateFileManager>();
        _testProxies = TestProxyGenerator.CreateTestProxies(2);
        _service = new ProxyReservationService(_lockerMock.Object, _testProxies, _fileManagerMock.Object);
    }

    [Fact]
    public async Task ReserveProxy_ShouldReturnProxyWithState_WhenProxyAvailable()
    {
        // Arrange
        var nodeAddress = "node_123";

        // Act
        var result = await _service.ReserveProxy(nodeAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeAddress, result!.ProxyState.NodeAddress);
        Assert.Contains(result.Proxy, _testProxies);
    }
    
    [Fact]
    public async Task ProlongProxy_ShouldUpdateReservationTime_WhenProxyExists()
    {
        // Arrange
        var proxy = _testProxies[0];
        var newDateTime = DateTime.Now.AddHours(1);
        await _service.ReserveProxy("node_123");

        // Act
        var result = await _service.ProlongProxy(proxy.Id, newDateTime);

        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task ProlongProxy_ShouldFail_WhenProxyNotExists()
    {
        // Arrange
        var newDateTime = DateTime.Now.AddHours(1);
        await _service.ReserveProxy("node_123");

        // Act
        var result = await _service.ProlongProxy(Guid.NewGuid(), newDateTime);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ReserveAndReleaseProxy_ShouldSucceed_WhenProxyIsReservedAndReleased()
    {
        // Arrange
        var nodeAddress = "node_123";
        var proxy = await _service.ReserveProxy(nodeAddress);
    
        Assert.NotNull(proxy);
        Assert.Equal(nodeAddress, proxy.ProxyState.NodeAddress);

        // Act
        var releaseKey = proxy.ProxyState.ReleaseKey;
        var releaseResult = await _service.ReleaseProxy(proxy.Proxy.Id, releaseKey);

        // Assert
        Assert.True(releaseResult);
    
        var reReservedProxy = await _service.ReserveProxy(nodeAddress);
    
        // Assert
        Assert.NotNull(reReservedProxy);
        Assert.NotEqual(proxy.Proxy.Id, reReservedProxy.Proxy.Id);
    }

    [Fact]
    public async Task ReleaseProxy_ShouldFail_WhenWrongReleaseKeyProvided()
    {
        // Arrange
        var nodeAddress = "node_123";
        var proxy = await _service.ReserveProxy(nodeAddress);

        Assert.NotNull(proxy);
        Assert.Equal(nodeAddress, proxy.ProxyState.NodeAddress);

        // Act
        var wrongReleaseKey = Guid.NewGuid();
        var releaseResult = await _service.ReleaseProxy(proxy.Proxy.Id, wrongReleaseKey);

        // Assert
        Assert.False(releaseResult);
    }
    
    [Fact]
    public async Task ReserveProxy_ShouldFail_WhenAllProxiesAreReserved()
    {
        // Arrange
        var nodeAddress = "node_123";

        foreach (var _ in _testProxies)
        {
            var result = await _service.ReserveProxy(nodeAddress);
            Assert.NotNull(result);
        }

        // Act
        var additionalResult = await _service.ReserveProxy(nodeAddress);

        // Assert
        Assert.Null(additionalResult);
    }
}

