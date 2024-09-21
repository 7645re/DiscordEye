using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using DiscordEye.ProxyDistributor.Services.SnapShoot;
using DiscordEye.ProxyDistributor.Services.Vault;
using Moq;

namespace DiscordEye.ProxyDistributor.Tests;

public class ProxyReservationServiceTests
{
    private Mock<IProxyVaultService> _proxyVaultServiceMock;
    private Mock<IProxyStateSnapShooter> _snapShooterMock;
    private Mock<KeyedLockService> _lockerMock;
    private ProxyReservationService _proxyReservationService;

    public ProxyReservationServiceTests()
    {
        _proxyVaultServiceMock = new Mock<IProxyVaultService>();
        _snapShooterMock = new Mock<IProxyStateSnapShooter>();
        _lockerMock = new Mock<KeyedLockService>();
        _snapShooterMock.Setup(s => s.LoadSnapShotAsync())
            .ReturnsAsync(() => null);
        _proxyReservationService = new ProxyReservationService(
            _lockerMock.Object,
            _proxyVaultServiceMock.Object,
            _snapShooterMock.Object);
        
        _snapShooterMock.Reset();
        _lockerMock.Reset();
        _proxyVaultServiceMock.Reset();
    }

    [Fact]
    public async Task ReserveProxy_ShouldReturnProxy_WhenAvailable()
    {
        // Arrange
        var nodeAddress = "127.0.0.1";
        var setupProxy = new[]
        {
            new ProxyVault(Guid.NewGuid(), "192.168.0.1", "8080", "user1", "pass1"),
            new ProxyVault(Guid.NewGuid(), "192.168.0.1", "8080", "user1", "pass1")
        };

        _proxyVaultServiceMock.Setup(s => s.GetAllProxiesAsync())
            .ReturnsAsync(setupProxy);
        _proxyReservationService = new ProxyReservationService(
            _lockerMock.Object,
            _proxyVaultServiceMock.Object,
            _snapShooterMock.Object);

        // Act
        var result = await _proxyReservationService.ReserveProxy(nodeAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeAddress, result.ProxyState.NodeAddress);
        Assert.Equal(setupProxy[0].Id, result.Proxy.Id);
        _proxyVaultServiceMock.Verify(x => x.GetAllProxiesAsync(), Times.Exactly(1));
        _snapShooterMock.Verify(x => x.SnapShootAsync(
            It.Is<IDictionary<Guid, ProxyState?>>(dict => dict.Count == 2)),
            Times.Once);
    }
    
    [Fact]
    public async Task ReserveProxy_ShouldReturnNull_WhenNoProxiesAvailable()
    {
        // Arrange
        _proxyVaultServiceMock.Setup(s => s.GetAllProxiesAsync())
            .ReturnsAsync(Array.Empty<ProxyVault>);
    
        var nodeAddress = "127.0.0.1";
    
        // Act
        var result = await _proxyReservationService.ReserveProxy(nodeAddress);
    
        // Assert
        Assert.Null(result);
    }
}