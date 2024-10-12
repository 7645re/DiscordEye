// using DiscordEye.EventsAggregator.Entities;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Caching.Memory;
//
// namespace DiscordEye.EventsAggregator.Services.DiscordUserService;
//
// public class DiscordUserService : IDiscordUserService
// {
//     private readonly IMemoryCache _memoryCache;
//     private readonly DiscordApiClient _discordApiClient;
//     private readonly ApplicationDbContext _applicationDbContext;
//
//     public DiscordUserService(
//         IMemoryCache memoryCache,
//         ApplicationDbContext applicationDbContext,
//         DiscordApiClient discordApiClient)
//     {
//         _memoryCache = memoryCache;
//         _applicationDbContext = applicationDbContext;
//         _discordApiClient = discordApiClient;
//     }
//
//     public async Task AddUserForTrackAsync(
//         ulong id,
//         CancellationToken cancellationToken = default)
//     {
//         var userIsExist = await _applicationDbContext
//             .Users
//             .AnyAsync(x => x.Id == id,
//                 cancellationToken);
//         if (userIsExist)
//             return;
//
//         var user = await _discordApiClient.GetUserAsync(
//             "http://localhost:5131",
//             id);
//         if (user is null)
//             throw new ArgumentException($"Cannot find user with id {id}");
//         
//         foreach (var guildResponse in user.Guilds)
//         {
//             await _applicationDbContext
//                 .Guilds
//                 .AddAsync(new GuildEntity
//                 {
//                     Id = ulong.Parse(guildResponse.Id),
//                     IconUrl = guildResponse.IconUrl,
//                     Name = guildResponse.Name
//                 }, cancellationToken);
//             await _applicationDbContext
//                 .UserGuilds
//                 .AddAsync(new UserGuildEntity
//                 {
//                     UserId = user.Id,
//                     GuildId = ulong.Parse(guildResponse.Id),
//                 }, cancellationToken);
//         }
//
//         var userEntity = new UserEntity
//         {
//             Id = user.Id,
//             Username = user.Username
//         };
//         await _applicationDbContext
//             .Users
//             .AddAsync(
//                 userEntity,
//                 cancellationToken);
//         
//         await _applicationDbContext.SaveChangesAsync(cancellationToken);
//     }
// }