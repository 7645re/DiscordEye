namespace DiscordEye.Shared.Extensions;

public static class StartupExtensions
{
    public static string GetDiscordTokenFromEnvironment()
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        
        if (token is null)
            throw new ArgumentException("The discord token was not specified in environment " +
                                    "variables with the DISCORD_TOKEN key");

        return token;
    }

    public static string GetPort()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
        
        if (port is null)
            throw new ArgumentException("You need to pass the port to " +
                                        "the ASPNETCORE_HTTP_PORTS environment variable");

        return port;
    }
}