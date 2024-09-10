using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Mappers;

public static class ProxyMapper
{
    public static bool TryToProxyDto(this IDictionary<string, object> data, out ProxyDto? proxyDto)
    {
        if (!data.TryGetValue("id", out var id)
            || !int.TryParse(id.ToString(), out var parsedId)
            || !data.TryGetValue("address", out var address)
            || !data.TryGetValue("port", out var port)
            || !data.TryGetValue("login", out var login)
            || !data.TryGetValue("password", out var password))
        {
            proxyDto = null;
            return false;
        }

        var stringAddress = address.ToString();
        var stringPort = port.ToString();
        var stringLogin = login.ToString();
        var stringPassword = password.ToString();

        if (stringAddress is null
            || stringPort is null
            || stringLogin is null
            || stringPassword is null)
        {
            proxyDto = null;
            return false;
        }

        proxyDto = new ProxyDto(
            parsedId,
            stringAddress,
            stringPort,
            stringLogin,
            stringPassword,
            null,
            null,
            true);
        return true;
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

    public static ProxyDto ToProxyDto(this Proxy proxy)
    {
        return new ProxyDto(
            proxy.Id,
            proxy.Address,
            proxy.Port,
            proxy.Login,
            proxy.Password,
            proxy.TakerAddress,
            proxy.TakenDateTime,
            proxy.IsFree());
    }

    public static Proxy ToProxy(this ProxyDto proxyDto)
    {
        return new Proxy(
            proxyDto.Id,
            proxyDto.Address,
            proxyDto.Port,
            proxyDto.Login,
            proxyDto.Password);
    }

    public static ProxyGrpc ToProxyGrpc(this ProxyDto proxyDto)
    {
        return new ProxyGrpc
        {
            Id = proxyDto.Id,
            Address = proxyDto.Address,
            Port = proxyDto.Port,
            Login = proxyDto.Login,
            Password = proxyDto.Password,
            TakerAddress = proxyDto.TakerAddress ?? string.Empty,
            IsFree = proxyDto.IsFree
        };
    }
}