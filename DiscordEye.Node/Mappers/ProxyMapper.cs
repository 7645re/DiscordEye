using System.Net;
using DiscordEye.Node.Dto;
using DiscordEye.ProxyDistributor;

namespace DiscordEye.Node.Mappers;

public static class ProxyMapper
{
    public static WebProxy ToWebProxy(this ReservedProxyGrpc reservedProxyGrpc)
    {
        return new WebProxy
        {
            Address = new Uri($"http://{reservedProxyGrpc.Address}:{reservedProxyGrpc.Port}"),
            Credentials = new NetworkCredential
            {
                Password = reservedProxyGrpc.Password,
                UserName = reservedProxyGrpc.Login
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