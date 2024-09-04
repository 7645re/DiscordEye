using System.Net;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using DiscordEye.Node.Dto;
using DiscordEye.Node.Mappers;
using DiscordEye.Node.Options;
using DiscordEye.ProxyDistributor;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public class DiscordRequestClient : IDiscordRequestClient
{
    private readonly ILogger<DiscordRequestClient> _logger;
    private DiscordSocketClient? _client;
    private readonly ProxyDistributorService.ProxyDistributorServiceClient _proxyDistributorService;
    private readonly DiscordOptions _options;
    private readonly string _serviceName;
    private readonly SemaphoreSlim _clientSemaphore = new(1,1);

    public DiscordRequestClient(
        ILogger<DiscordRequestClient> logger,
        ProxyDistributorService.ProxyDistributorServiceClient proxyDistributorService,
        DiscordOptions options)
    {
        _logger = logger;
        _proxyDistributorService = proxyDistributorService;
        _options = options;
        _serviceName = GetServiceName();
        _client = InitClientAsync().GetAwaiter().GetResult();
    }

    private static string GetServiceName()
    {
        const string serviceNameKey = "SERVICE_NAME";
        var serviceName = Environment.GetEnvironmentVariable(serviceNameKey);
        if (serviceName is null)
            throw new ArgumentNullException($"The {serviceNameKey} variable was not specified" +
                                            $" in the environment variables");

        return serviceName;
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
        return await RetryOnFailureAsync(async () =>
        {
            // TODO: maybe pass semaphore deep into RetryOnFailureAsync 
            await _clientSemaphore.WaitAsync();
            try
            {
                ThrowIfClientNotInitialized();
                var userProfile = await _client?.Rest.GetUserProfileAsync(id);
                return userProfile.ToDiscordUser();
            }
            finally
            {
                _clientSemaphore.Release();
            }
        });
    }
    
    private async Task<T> RetryOnFailureAsync<T>(Func<Task<T>> action)
    {
        const int maxRetryAttempts = 3;
        var attempt = 0;

        while (attempt < maxRetryAttempts)
        {
            try
            {
                return await action();
            }
            catch (CloudFlareException ex)
            {
                _logger.LogWarning(ex, "CloudFlareException occurred. Retrying...");

                if (++attempt >= maxRetryAttempts)
                {
                    _logger.LogError("Max retry attempts reached. Could not complete the operation.");
                    throw;
                }

                // TODO: wait until get proxy
                var webProxy = await _proxyDistributorService.TakeProxyAsync(new ProxyRequest
                {
                    ServiceName = _serviceName
                });
                await _client.DisposeAsync();
                _logger.LogInformation($"{nameof(DiscordSocketClient)} disposed in {nameof(DiscordRequestClient)}");
                _client = await InitClientAsync(webProxy.ToWebProxy());
                _logger.LogInformation($"{nameof(DiscordSocketClient)} reinit with proxy address " +
                                       $"{webProxy.Proxy.Address}");
            }
        }

        throw new InvalidOperationException("Unexpected end of retry loop");
    }
}