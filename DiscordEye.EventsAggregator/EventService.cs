using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace DiscordEye.EventsAggregator;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly DiscordApiClient _discordApiClient;

    public EventService(
        ApplicationDbContext applicationDbContext,
        DiscordApiClient discordApiClient)
    {
        _applicationDbContext = applicationDbContext;
        _discordApiClient = discordApiClient;
    }

    public async Task AddReceivedMessageAsync(
        MessageReceivedEvent messageReceivedEvent,
        CancellationToken cancellationToken)
    {
        await AddUserIfDoesntExist(
            messageReceivedEvent.UserId,
            cancellationToken);
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
        
        await AddChannelIfDoesntExist(
            messageReceivedEvent.GuildId,
            messageReceivedEvent.ChannelId,
            cancellationToken);
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
        
        var message = new MessageEntity
        {
            Id = messageReceivedEvent.MessageId,
            GuildId = messageReceivedEvent.GuildId,
            ChannelId = messageReceivedEvent.ChannelId,
            UserId = messageReceivedEvent.UserId,
            Content = messageReceivedEvent.Content,
            IsDeleted = false
        };
        await _applicationDbContext.AddAsync(message, cancellationToken);
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddUserIfDoesntExist(long id, CancellationToken cancellationToken)
    {
        var userIsExist = await _applicationDbContext
            .Users
            .AnyAsync(x => x.Id == id, cancellationToken);
        if (userIsExist)
            return;
        
        var user = await _discordApiClient.GetUserAsync(
            "http://localhost:5131",
            id,
            cancellationToken);
        if (user is null)
            throw new ArgumentException($"Cannot find user with id {id}");

        foreach (var guildResponse in user.Guilds)
            await AddGuildIfDoesntExist(guildResponse.Id, cancellationToken);

        var userEntity = new UserEntity
        {
            Id = user.Id,
            Username = user.Username,
        };
        await _applicationDbContext
            .Users
            .AddAsync(userEntity, cancellationToken);
    }

    private async Task AddChannelIfDoesntExist(
        long guildId,
        long channelId,
        CancellationToken cancellationToken)
    {
        var channelIsExist = await _applicationDbContext
            .Channels
            .AnyAsync(x => x.Id == channelId, cancellationToken);
        
        if (channelIsExist)
            return;

        var channel = await _discordApiClient.GetChannelAsync(
            "http://localhost:5131",
            channelId,
            cancellationToken);
        if (channel is null)
            throw new ArgumentException($"Cannot find channel with id {channelId}");

        var channelEntity = new ChannelEntity
        {
            Id = channel.Id,
            Name = channel.Name,
            GuildId = guildId
        };
        await _applicationDbContext
            .Channels
            .AddAsync(channelEntity, cancellationToken);
    }

    private async Task AddGuildIfDoesntExist(long id, CancellationToken cancellationToken)
    {
        var guildIsExist = await _applicationDbContext
            .Guilds
            .AnyAsync(x => x.Id == id, cancellationToken);
        
        if (guildIsExist)
            return;

        var guild = await _discordApiClient.GetGuildAsync(
            "http://localhost:5131",
            id,
            cancellationToken);
        if (guild is null)
            throw new ArgumentException($"Cannot find guild with id {id}");
        
        var guildEntity = new GuildEntity
        {
            Id = guild.Id,
            Name = guild.Name,
            IconUrl = guild.IconUrl
        };
        await _applicationDbContext
            .Guilds
            .AddAsync(guildEntity,
                cancellationToken);
    }
}