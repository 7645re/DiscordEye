using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordEye.Shared.Extensions;

public static class ServiceProviderExtensions
{
    public static TWorkerType? GetHostedService<TWorkerType>
        (this IServiceProvider serviceProvider) =>
        serviceProvider
            .GetServices<IHostedService>()
            .OfType<TWorkerType>()
            .FirstOrDefault();
}