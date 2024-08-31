using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public interface IProxyStorageService
{
    Proxy? TakeProxy(string serviceName);
}