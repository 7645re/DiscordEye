using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace DiscordEye.EventsAggregator;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly DiscordNodeApiClient _discordNodeApiClient;

    public EventService(
        ApplicationDbContext applicationDbContext,
        DiscordNodeApiClient discordNodeApiClient)
    {
        _applicationDbContext = applicationDbContext;
        _discordNodeApiClient = discordNodeApiClient;
    }

    public async Task AddReceivedMessageAsync(
        MessageReceivedEvent messageReceivedEvent,
        CancellationToken cancellationToken)
    {
        await AddUserIfDoesntExist(
            messageReceivedEvent.NodeId,
            messageReceivedEvent.UserId,
            cancellationToken);
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
        
        await AddChannelIfDoesntExist(
            messageReceivedEvent.NodeId,
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

    private async Task AddUserIfDoesntExist(
        int nodeId,
        long id,
        CancellationToken cancellationToken)
    {
        var userIsExist = await _applicationDbContext
            .Users
            .AnyAsync(x => x.Id == id, cancellationToken);
        if (userIsExist)
            return;
        
        var user = await _discordNodeApiClient.GetUserFromAllNodesAsync(
            id,
            cancellationToken);
        if (user is null)
            throw new ArgumentException($"Cannot find user with id {id}");

        foreach (var guildResponse in user.Guilds)
            await AddGuildIfDoesntExist(nodeId, long.Parse(guildResponse.Id), cancellationToken);

        var guildEntities = user.Guilds.Select(x => new GuildEntity
        {
            Id = long.Parse(x.Id)
        });
        
        var userEntity = new UserEntity
        {
            Id = long.Parse(user.Id),
            Username = user.Username,
            GuildsEntities = guildEntities.ToList()
        };
        await _applicationDbContext
            .Users
            .AddAsync(userEntity, cancellationToken);
    }

    private async Task AddChannelIfDoesntExist(
        int nodeId,
        long guildId,
        long channelId,
        CancellationToken cancellationToken)
    {
        var channelIsExist = await _applicationDbContext
            .Channels
            .AnyAsync(x => x.Id == channelId, cancellationToken);
        
        if (channelIsExist)
            return;

        var channel = await _discordNodeApiClient.GetChannelAsync(
            nodeId,
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

    private async Task AddGuildIfDoesntExist(
        int nodeId,
        long id,
        CancellationToken cancellationToken)
    {
        var guildIsExist = await _applicationDbContext
            .Guilds
            .AnyAsync(x => x.Id == id, cancellationToken);
        
        if (guildIsExist)
            return;

        var guild = await _discordNodeApiClient.GetGuildAsync(
            nodeId,
            id,
            cancellationToken);
        if (guild is null)
            throw new ArgumentException($"Cannot find guild with id {id}");
        
        var guildEntity = new GuildEntity
        {
            Id = long.Parse(guild.Id),
            Name = guild.Name,
            IconUrl = guild.IconUrl
        };
        await _applicationDbContext
            .Guilds
            .AddAsync(guildEntity,
                cancellationToken);
    }
}