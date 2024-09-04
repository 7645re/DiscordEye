using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Mappers;

public static class ProxyMapper
{
    public static Proxy ToProxy(this Options.ProxyInfo proxyInfo)
    {
        return new Proxy(
            proxyInfo.Id,
            proxyInfo.Address,
            proxyInfo.Port,
            proxyInfo.Login,
            proxyInfo.Password);
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
            IsFree = proxy.IsFree()
        };
    }
}