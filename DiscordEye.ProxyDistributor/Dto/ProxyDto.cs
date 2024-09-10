namespace DiscordEye.ProxyDistributor.Dto;

public record ProxyDto(
    int Id,
    string Address,
    string Port,
    string Login,
    string Password,
    string? TakerAddress,
    DateTime? TakenDateTime,
    bool IsFree);