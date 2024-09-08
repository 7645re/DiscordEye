using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Mappers;

public static class ProxyMapper
{
    public static Proxy ToProxy(this ProxyInfo proxy)
    {
        return new Proxy(
            proxy.Id,
            proxy.Address,
            proxy.Port,
            proxy.Login,
            proxy.Password);
    }

    public static TakenProxy ToTakenProxy(this (Proxy proxy, Guid releaseKey) proxyWithKey)
    {
        return new TakenProxy
        {
            Id = proxyWithKey.proxy.Id,
            Address = proxyWithKey.proxy.Address,
            Port = proxyWithKey.proxy.Port,
            Login = proxyWithKey.proxy.Login,
            Password = proxyWithKey.proxy.Password,
            ReleaseKey = proxyWithKey.releaseKey.ToString()
        };
    }

    public static ProxyInfo ToProxyInfo(this Proxy proxy)
    {
        return new ProxyInfo
        {
            Id = proxy.Id,
            Address = proxy.Address,
            Port = proxy.Port,
            Login = proxy.Login,
            Password = proxy.Password,
            NodeAddress = proxy.TakerAddress ?? string.Empty,
            IsFree = proxy.IsFree()
        };
    }
}