using System.Net;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using DiscordEye.Node.Dto;
using DiscordEye.Node.Mappers;
using DiscordEye.Node.Options;
using DiscordEye.ProxyDistributor;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public class DiscordRequestClient : IDiscordRequestClient
{
    private readonly ILogger<DiscordRequestClient> _logger;
    private DiscordSocketClient? _client;
    private readonly ProxyDistributorService.ProxyDistributorServiceClient _proxyDistributorService;
    private readonly DiscordOptions _options;
    private readonly SemaphoreSlim _clientSemaphore = new(1,1);

    public DiscordRequestClient(
        ILogger<DiscordRequestClient> logger,
        ProxyDistributorService.ProxyDistributorServiceClient proxyDistributorService,
        IOptions<DiscordOptions> options)
    {
        _logger = logger;
        _proxyDistributorService = proxyDistributorService;
        _options = options.Value;
        _client = InitClientAsync().GetAwaiter().GetResult();
    }

    public async Task<DiscordSocketClient> InitClientAsync(WebProxy? webProxy = null)
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        if (webProxy is not null)
            discordSocketConfig.RestClientProvider = DefaultRestClientProvider.Create(webProxy: webProxy);

        var client = new DiscordSocketClient(discordSocketConfig);
        await client.LoginAsync(TokenType.User, _options.Token);
        await client.StartAsync();
        _logger.LogInformation($"{nameof(DiscordRequestClient)} complete started");
        return client;
    }

    private void ThrowIfClientNotInitialized()
    {
        if (_client is null)
            throw new ArgumentException($"{nameof(DiscordSocketClient)} not initialize");
    }
    
    public async Task<DiscordUser> GetUserAsync(ulong id)
    {
        return await RetryOnFailureUseProxyAsync(async () =>
        {
            ThrowIfClientNotInitialized();
            var userProfile = await _client?.Rest.GetUserProfileAsync(id);
            return userProfile.ToDiscordUser();
        });
    }

    public async Task<WebProxy?> TakeProxyInLoopAsync(
        int retryCount = 0,
        int millisecondsDelay = 0)
    {
        var counter = 0;
        while (counter < retryCount)
        {
            var webProxyResponse = await _proxyDistributorService.TakeProxyAsync(new TakeProxyRequest());
            if (webProxyResponse.Proxy is not null)
                return webProxyResponse.ToWebProxy();

            counter++;
            if (millisecondsDelay == 0) continue;
            await Task.Delay(millisecondsDelay);
        }

        return null;
    }
    
    public async Task<T?> RetryOnFailureUseProxyAsync<T>(
        Func<Task<T>> action,
        int retryCount = 1,
        int millisecondDelay = 0)
    {
        var counter = 0;
        while (counter < retryCount)
        {
            try
            {
                return await ExecuteInSemaphoreWithoutExceptionAsync(async () => await action());
            }
            catch (CloudFlareException e)
            {
                var proxy = await TakeProxyInLoopAsync(5, 2000);
                if (proxy is null)
                    continue;

                await ExecuteInSemaphoreWithoutExceptionAsync(async () =>
                {
                    if (_client != null)
                    {
                        await _client.LogoutAsync();
                        await _client.DisposeAsync();
                    }
                    _client = await InitClientAsync(proxy);
                    return Task.CompletedTask;
                });
            }

            await Task.Delay(millisecondDelay);
            counter++;
        }

        if (_client is null)
        {
            await ExecuteInSemaphoreWithoutExceptionAsync(async () =>
            {
                _client = await InitClientAsync();
                return Task.CompletedTask;
            });
        }
        
        return default;
    }
    
    private async Task<T> ExecuteInSemaphoreWithoutExceptionAsync<T>(Func<Task<T>> action)
    {
        Exception? exception;
        await _clientSemaphore.WaitAsync();
        try
        {
            return await action();
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            _clientSemaphore.Release();
        }

        throw exception;
    }
}