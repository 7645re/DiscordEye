namespace DiscordEye.ProxyDistributor.Data;

public record Proxy(Guid Id, string Address, string Port, string Login, string Password);