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
            proxyInfo.Password,
            false,
            null);
    }

    public static ProxyInfo ToProxyInfo(this Proxy proxy)
    {
        return new ProxyInfo
        {
            Address = proxy.Address,
            Port = proxy.Port,
            Login = proxy.Login,
            Password = proxy.Password
        };
    }
}