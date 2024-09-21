using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Dto;

public record ProxyWithReleaseKey(Proxy Proxy, Guid ReleaseKey);