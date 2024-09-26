using System.Net;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using DiscordEye.Node.Data;
using DiscordEye.Node.Mappers;
using DiscordEye.Node.Services;
using DiscordEye.ProxyDistributor;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public class DiscordRequestClient : IDiscordRequestClient
{
    private DiscordSocketClient? _client;
    private readonly SemaphoreSlim _clientSemaphore = new(1,1);
    private readonly string _token;
    private readonly ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient _distributorGrpcServiceClient;
    private readonly IProxyHolderService _proxyHolderService;
    private readonly ILogger<DiscordRequestClient> _logger;

    public DiscordRequestClient(
        ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient distributorGrpcServiceClient,
        IProxyHolderService proxyHolderService, ILogger<DiscordRequestClient> logger)
    {
        _token = StartupExtensions.GetDiscordTokenFromEnvironment();
        _distributorGrpcServiceClient = distributorGrpcServiceClient;
        _proxyHolderService = proxyHolderService;
        _logger = logger;
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
        await client.LoginAsync(TokenType.User, _token);
        await client.StartAsync();
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

    private async Task<Proxy?> ReserveProxyInLoopAsync(
        int retryCount = 0,
        int millisecondsDelay = 0)
    {
        var counter = 0;
        while (counter < retryCount)
        {
            _logger.LogInformation($"Attempt number {counter + 1} proxy reservation");
            var reservedProxy = await _proxyHolderService.ReserveProxyAndReleaseIfNeeded();

            if (reservedProxy is not null)
                return reservedProxy;

            counter++;
            await Task.Delay(millisecondsDelay);
        }

        return null;
    }

    private async Task<T?> RetryOnFailureUseProxyAsync<T>(
        Func<Task<T>> action,
        int retryCount = 2,
        int millisecondDelay = 0)
    {
        var counter = 0;
        while (counter < retryCount)
        {
            counter++;
            try
            {
                if (new Random().Next(0, 2) == 1)
                {
                    throw new CloudFlareException();
                }
                return await ExecuteInClientSemaphoreAsync(async () => await action());
            }
            catch (CloudFlareException e)
            {
                var proxy = await ReserveProxyInLoopAsync(5, 2000);
                if (proxy is null)
                    continue;

                await ExecuteInClientSemaphoreAsync(async () =>
                {
                    if (_client != null)
                    {
                        await _client.LogoutAsync();
                        await _client.DisposeAsync();
                    }
                    _client = await InitClientAsync(proxy.ToWebProxy());
                    return Task.CompletedTask;
                });
            }

            await Task.Delay(millisecondDelay);
        }

        return default;
    }
    
    private async Task<T?> ExecuteInClientSemaphoreAsync<T>(Func<Task<T>> action)
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

        if (exception is not null)
            throw exception;

        return default;
    }
}