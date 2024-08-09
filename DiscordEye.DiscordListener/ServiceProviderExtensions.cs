namespace DiscordEye.DiscordListener;

public static class ServiceProviderExtensions
{
    public static TWorkerType? GetHostedService<TWorkerType>
        (this IServiceProvider serviceProvider) =>
        serviceProvider
            .GetServices<IHostedService>()
            .OfType<TWorkerType>()
            .FirstOrDefault();
}