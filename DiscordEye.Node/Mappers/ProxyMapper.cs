using System.Net;
using DiscordEye.Node.Data;

namespace DiscordEye.Node.Mappers;

public static class ProxyMapper
{
    public static WebProxy ToWebProxy(this Proxy proxy)
    {
        return new WebProxy
        {
            Address = new Uri($"http://{proxy.Address}:{proxy.Port}"),
            Credentials = new NetworkCredential
            {
                Password = proxy.Password,
                UserName = proxy.Login
            }
        };
    }

    public static Proxy ToProxy(this ReservedProxyGrpc reservedProxyGrpc)
    {
        return new Proxy
        {
            Id = Guid.Parse(reservedProxyGrpc.Id),
            Address = reservedProxyGrpc.Address,
            Port = reservedProxyGrpc.Port,
            Login = reservedProxyGrpc.Login,
            Password = reservedProxyGrpc.Password,
            ReleaseKey = Guid.Parse(reservedProxyGrpc.ReleaseKey)
        };
    }
}