using System.Collections.Concurrent;
using System.Text.Json;

public class ShardedKeyValueStore
{
    private const int ShardPower = 4;           
    private const int ShardCount = 1 << ShardPower; // 16 shards
    private const int ShardMask = ShardCount - 1;

    private readonly ConcurrentDictionary<string, string>[] _shards =
        Enumerable.Range(0, ShardCount)
                  .Select(_ => new ConcurrentDictionary<string, string>())
                  .ToArray();

    private static int Idx(string k) => k.GetHashCode() & ShardMask;

    public bool TryAdd(string k, string v) => _shards[Idx(k)].TryAdd(k, v);
    public bool TryGet(string k, out string v) => _shards[Idx(k)].TryGetValue(k, out v);
    public bool TryRemove(string k) => _shards[Idx(k)].TryRemove(k, out _);
    public bool TryUpdate(string k, string v)
    {
        var shard = _shards[Idx(k)];
        if (shard.TryGetValue(k, out var old))
            return shard.TryUpdate(k, v, old);
        return false;                            
    }

    public (int added, List<string> exists) AddMany(Dictionary<string, string> kv)
    {
        var duplicates = new List<string>();
        int added = 0;
        foreach (var (k, v) in kv)
            if (!TryAdd(k, v)) duplicates.Add(k); else added++;
        return (added, duplicates);
    }

    public (int updated, List<string> absent) UpdateMany(Dictionary<string, string> kv)
    {
        var missing = new List<string>();
        int updated = 0;
        foreach (var (k, v) in kv)
            if (TryUpdate(k, v)) updated++; else missing.Add(k);
        return (updated, missing);
    }

    public (int removed, List<string> absent) RemoveMany(IEnumerable<string> keys)
    {
        var missing = new List<string>();
        int removed = 0;
        foreach (var k in keys)
            if (TryRemove(k)) removed++; else missing.Add(k);
        return (removed, missing);
    }

    public Dictionary<string, string> Snapshot() =>
        _shards.SelectMany(s => s).ToDictionary(e => e.Key, e => e.Value);

    public void ClearAll() { foreach (var s in _shards) s.Clear(); }

    public void Dump(string path)
    {
        var json = JsonSerializer.Serialize(Snapshot());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    public void Load(string path)
    {
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new();
        ClearAll();
        foreach (var (k, v) in data)
            _shards[Idx(k)][k] = v;
    }
}
