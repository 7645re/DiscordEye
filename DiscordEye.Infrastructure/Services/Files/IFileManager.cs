namespace DiscordEye.Infrastructure.Services.Files;

public interface IFileManager<TData>
    where TData : class
{
    Task Write(IEnumerable<TData> takerNode);
    Task Append(TData takerNode);
    Task<IReadOnlyCollection<TData>> Read();
    Task RemoveBy(Func<TData, bool> predicate);
}
