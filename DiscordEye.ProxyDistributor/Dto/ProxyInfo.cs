namespace DiscordEye.ProxyDistributor.Dto;

public class ProxyInfo
{
    public required int Id { get; set; }
    public required string Address { get; set; }
    public required string Port { get; set; }
    public required string Login { get; set; }
    public required string Password { get; set; }
}