using System.Net;
using DiscordEye.Node.Dto;
using DiscordEye.ProxyDistributor;

namespace DiscordEye.Node.Mappers;

public static class ProxyMapper
{
    public static WebProxy ToWebProxy(this TakenProxy takenProxy)
    {
        return new WebProxy
        {
            Address = new Uri($"http://{takenProxy.Address}:{takenProxy.Port}"),
            Credentials = new NetworkCredential
            {
                Password = takenProxy.Password,
                UserName = takenProxy.Login
            }
        };
    }

    public static Proxy ToProxy(this TakenProxy takenProxy)
    {
        return new Proxy
        {
            Id = takenProxy.Id,
            Address = takenProxy.Address,
            Port = takenProxy.Port,
            Login = takenProxy.Login,
            Password = takenProxy.Password,
            ReleaseKey = Guid.Parse(takenProxy.ReleaseKey)
        };
    }
}