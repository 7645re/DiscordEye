using System.Text.Json;

namespace DiscordEye.Infrastructure.Services.Files;

public abstract class BaseJsonFileManager<TData> : IFileManager<TData>
    where TData : class
{
    protected abstract string FilePath { get; }

    public async Task Write(IEnumerable<TData> takerNodes)
    {
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(takerNodes));
    }

    public async Task Append(TData takerNode)
    {
        var takerNodes = await Read();

        await Write(takerNodes.Append(takerNode));
    }

    public async Task<IReadOnlyCollection<TData>> Read()
    {
        return JsonSerializer.Deserialize<List<TData>>(await File.ReadAllTextAsync(FilePath)) ?? [];
    }

    public async Task RemoveBy(Func<TData, bool> predicate)
    {
        var takerNodes = await Read();

        await Write(takerNodes.Where(x => predicate(x) == false));
    }
}
