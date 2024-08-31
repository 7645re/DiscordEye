using DiscordEye.Shared.DiscordListenerApi.Response;

namespace DiscordEye.EventsAggregator;

public static class DiscordNodeApiClientHelper
{
    public static DiscordUserResponse ConcatUsersFromNodes(List<DiscordUserResponse> userResponses)
    {
        switch (userResponses.Count)
        {
            case 0:
                throw new ArgumentException("Cannot concat less than one element");
            case 1:
                return userResponses.First();
        }

        var userResponse = userResponses.First();
        var guilds = userResponse.Guilds.ToList();
        for (var i = 1; i < userResponses.Count; i++)
            guilds.AddRange(userResponse.Guilds);

        var uniqueGuilds = guilds.DistinctBy(x => x.Id);
        userResponse.Guilds = uniqueGuilds.ToList();
        return userResponse;
    }
}