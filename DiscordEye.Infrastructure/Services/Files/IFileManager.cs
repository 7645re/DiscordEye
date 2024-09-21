namespace DiscordEye.Infrastructure.Services.Files;

public interface IFileManager<TData>
    where TData : class
{
    Task Write(IEnumerable<TData> data);
    Task Append(TData data);
    Task<IReadOnlyCollection<TData>> Read();
    Task RemoveBy(Func<TData, bool> predicate);
}
