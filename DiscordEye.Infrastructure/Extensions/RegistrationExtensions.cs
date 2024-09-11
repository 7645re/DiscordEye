using DiscordEye.Infrastructure.Services.Lock;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordEye.Infrastructure.Extensions;

public static class RegistrationExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<KeyedLockService>();

        return services;
    }
}
