using System.Net;
using DiscordEye.ProxyDistributor;

namespace DiscordEye.Node.Mappers;

public static class ProxyMapper
{
    public static WebProxy ToWebProxy(this ProxyResponse proxyResponse)
    {
        return new WebProxy
        {
            Address = new Uri($"{proxyResponse.Proxy.Address}:{proxyResponse.Proxy.Port}"),
            Credentials = new NetworkCredential
            {
                Password = proxyResponse.Proxy.Password,
                UserName = proxyResponse.Proxy.Login
            }
        };
    }
}