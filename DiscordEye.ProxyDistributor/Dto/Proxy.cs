namespace DiscordEye.ProxyDistributor.Dto;

public class Proxy
{
    public readonly int Id;
    public readonly string Address;
    public readonly string Port;
    public readonly string Login;
    public readonly string Password;
    private bool _isUsing;
    public string? WhoUsing;

    public Proxy(
        int id,
        string address,
        string port,
        string login,
        string password,
        bool isUsing,
        string? whoUsing)
    {
        Address = address;
        Port = port;
        Login = login;
        Password = password;
        _isUsing = isUsing;
        WhoUsing = whoUsing;
        Id = id;
    }

    public bool IsFree()
    {
        return !_isUsing;    
    }

    public bool Take(string name)
    {
        if (!IsFree())
            return false;

        WhoUsing = name;
        _isUsing = true;
        return true;
    }

    public bool Release()
    {
        if (IsFree())
            return false;

        WhoUsing = null;
        _isUsing = false;
        return true;
    }
}