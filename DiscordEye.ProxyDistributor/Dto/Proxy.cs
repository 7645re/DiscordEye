namespace DiscordEye.ProxyDistributor.Dto;

public class Proxy
{
    public readonly int Id;
    public readonly string Address;
    public readonly string Port;
    public readonly string Login;
    public readonly string Password;
    public string? TakerAddress;
    public DateTime? TakenDateTime;
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

    public bool TryTake(string takerAddress, out Guid? releaseKey)
    {
        if (string.IsNullOrEmpty(takerAddress))
            throw new ArgumentException("Taker address cannot be null or empty");
        
        lock (_lockObjet)
        {
            if (!IsFree())
            {
                releaseKey = null;
                return false;
            }

            TakerAddress = takerAddress;
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

            TakerAddress = null;
            _releaseKey = null;
            return true;    
        }
    }
}