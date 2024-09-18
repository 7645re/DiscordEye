using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Mappers;

public static class ProxyMapper
{
    public static bool TryToProxyVault(this IDictionary<string, object> data, out ProxyVault? proxyVault)
    {
        if (
            !data.TryGetValue("id", out var id)
            || !Guid.TryParse(id.ToString(), out var parsedId)
            || !data.TryGetValue("address", out var address)
            || !data.TryGetValue("port", out var port)
            || !data.TryGetValue("login", out var login)
            || !data.TryGetValue("password", out var password)
        )
        {
            proxyVault = null;
            return false;
        }

        var stringAddress = address.ToString();
        var stringPort = port.ToString();
        var stringLogin = login.ToString();
        var stringPassword = password.ToString();

        if (
            stringAddress is null
            || stringPort is null
            || stringLogin is null
            || stringPassword is null
        )
        {
            proxyVault = null;
            return false;
        }

        proxyVault = new ProxyVault(
            parsedId,
            stringAddress,
            stringPort,
            stringLogin,
            stringPassword);
        return true;
    }

    public static Proxy ToProxy(this ProxyVault proxyVault)
    {
        return new Proxy(
            proxyVault.Id,
            proxyVault.Address,
            proxyVault.Port,
            proxyVault.Login,
            proxyVault.Password);
    }

    public static ReservedProxyGrpc ToReservedProxy(this ProxyWithProxyState proxyWithProxyState)
    {
        return new ReservedProxyGrpc
        {
            Id = proxyWithProxyState.Proxy.Id.ToString(),
            Address = proxyWithProxyState.Proxy.Address,
            Port = proxyWithProxyState.Proxy.Port,
            Login = proxyWithProxyState.Proxy.Login,
            Password = proxyWithProxyState.Proxy.Password,
            ReleaseKey = proxyWithProxyState.ProxyState.ReleaseKey.ToString()
        };
    }

    public static ProxyHeartbeat ToProxyHeartbeat(this ProxyWithProxyState proxyWithProxyState)
    {
        return new ProxyHeartbeat(
            proxyWithProxyState.Proxy.Id,
            proxyWithProxyState.ProxyState.ReleaseKey,
            proxyWithProxyState.ProxyState.NodeAddress,
            proxyWithProxyState.ProxyState.LastReservationTime);
    }
}
