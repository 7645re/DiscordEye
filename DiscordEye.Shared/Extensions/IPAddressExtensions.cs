using System.Net;
using System.Net.Sockets;

namespace DiscordEye.Shared.Extensions;

public static class IpAddressExtensions
{
    public static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Local IP address not found!");
    }
}