using AsyncKeyedLock;

namespace DiscordEye.Infrastructure.Services.Lock;

public class KeyedLockService : AsyncKeyedLocker<string>;
