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
            discordSocketConfig.RestClientProvider = DefaultRestClientProvider.Create(
                webProxy: webProxy,
                useProxy: true);

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
    
    public async Task<DiscordUser?> GetUserAsync(ulong id)
    {
        return await RetryOnFailureUseProxyAsync(async () =>
        {
            ThrowIfClientNotInitialized();
            var userProfile = await _client?.Rest.GetUserProfileAsync(id);
            return userProfile?.ToDiscordUser();
        }, retryCount: 2);
    }

    // TODO: sync execution in task [refactor]
    public async Task<DiscordGuild?> GetGuildAsync(ulong id)
    {
        return await RetryOnFailureUseProxyAsync(() =>
        {
            ThrowIfClientNotInitialized();
            var guild = _client?.GetGuild(id);
            return Task.FromResult(guild?.ToDiscordGuild());
        });
    }

    private async Task<WebProxy?> TakeProxyInLoopAsync(
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

    private async Task<T?> RetryOnFailureUseProxyAsync<T>(
        Func<Task<T>> action,
        int retryCount = 1,
        int millisecondDelay = 0)
    {
        var counter = 0;
        while (counter < retryCount)
        {
            try
            {
                return await ExecuteInClientSemaphoreAsync(async () => await action());
            }
            catch (CloudFlareException e)
            {
                var proxy = await TakeProxyInLoopAsync(5, 2000);
                if (proxy is null)
                    continue;

                _logger.LogInformation("Proxy taken: {Proxy}", proxy);
                await ExecuteInClientSemaphoreAsync(async () =>
                {
                    if (_client != null)
                    {
                        await _client.LogoutAsync();
                        await _client.DisposeAsync();
                    }
                    _client = await InitClientAsync(proxy);
                    _logger.LogInformation("Reinitialize client with proxy complete");
                    return Task.CompletedTask;
                });
            }

            await Task.Delay(millisecondDelay);
            counter++;
        }

        return default;
    }
    
    private async Task<T> ExecuteInClientSemaphoreAsync<T>(Func<Task<T>> action)
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