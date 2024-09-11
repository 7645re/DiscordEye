namespace DiscordEye.ProxyDistributor.Data;

public class Proxy
{
    //TODO: make Guid
    public readonly int Id;

    public readonly string Address;
    public readonly string Port;
    public readonly string Login;
    public readonly string Password;
    public string? TakerAddress;
    public DateTime? TakenDateTime;
    public Guid? ReleaseKey;

    public Proxy(int id, string address, string port, string login, string password)
    {
        Address = address;
        Port = port;
        Login = login;
        Password = password;
        Id = id;
    }

    //TODO: refactor move to ProxyInfoProvider
    public bool IsFree()
    {
        return ReleaseKey is null && TakerAddress is null && TakenDateTime is null;
    }

    //TODO: refactor move to ProxyInfoProvider
    public bool EqualsReleaseKey(Guid releaseKey)
    {
        return ReleaseKey == releaseKey;
    }
}
