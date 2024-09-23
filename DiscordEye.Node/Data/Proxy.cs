namespace DiscordEye.Node.Data;

public class Proxy
{
    public required Guid Id { get; init; }
    public required string Address { get; init; }
    public required string Port { get; init; }
    public required string Login { get; init; }
    public required string Password { get; init; }
    public required Guid ReleaseKey { get; init; }
}