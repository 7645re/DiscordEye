using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Tests;

using System;

public static class TestProxyGenerator
{
    private static readonly Random Random = new Random();

    public static Proxy CreateTestProxy()
    {
        return new Proxy(
            Guid.NewGuid(),
            GenerateIpAddress(),
            GeneratePort(),
            GenerateLogin(),
            GeneratePassword());
    }

    public static Proxy[] CreateTestProxies(int count)
    {
        var proxies = new Proxy[count];
        for (int i = 0; i < count; i++)
        {
            proxies[i] = CreateTestProxy();
        }
        return proxies;
    }

    private static string GenerateIpAddress()
    {
        return $"{Random.Next(1, 255)}.{Random.Next(1, 255)}.{Random.Next(1, 255)}.{Random.Next(1, 255)}";
    }

    private static string GeneratePort()
    {
        return Random.Next(1024, 65535).ToString();
    }

    private static string GenerateLogin()
    {
        var logins = new[] { "user1", "user2", "testUser", "admin", "proxyUser" };
        return logins[Random.Next(logins.Length)];
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var password = new char[10];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[Random.Next(chars.Length)];
        }
        return new string(password);
    }
}
