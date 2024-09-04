namespace DiscordEye.ProxyDistributor.Dto;

public class Proxy
{
    public readonly int Id;
    public readonly string Address;
    public readonly string Port;
    public readonly string Login;
    public readonly string Password;
    private Guid? _releaseKey;
    private readonly object _lockObjet = new();

    public Proxy(
        int id,
        string address,
        string port,
        string login,
        string password)
    {
        Address = address;
        Port = port;
        Login = login;
        Password = password;
        Id = id;
    }

    public bool IsFree()
    {
        return _releaseKey is null;
    }

    public bool TryTake(out Guid? releaseKey)
    {
        lock (_lockObjet)
        {
            if (!IsFree())
            {
                releaseKey = null;
                return false;
            }

            _releaseKey = Guid.NewGuid();
            releaseKey = _releaseKey;
            return true;    
        }
    }

    public bool TryRelease(Guid releaseKey)
    {
        lock (_lockObjet)
        {
            if (IsFree() || _releaseKey!.Value != releaseKey)
                return false;

            _releaseKey = null;
            return true;    
        }
    }
}