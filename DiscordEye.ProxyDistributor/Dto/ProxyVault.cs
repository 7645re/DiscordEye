namespace DiscordEye.ProxyDistributor.Dto;

public record ProxyVault(
    Guid Id,
    string Address,
    string Port,
    string Login,
    string Password);