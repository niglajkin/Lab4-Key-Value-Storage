using Xunit;

public class StoreTests
{
    [Fact]
    public void Add_Get_Update_Remove()
    {
        var s = new ShardedKeyValueStore();
        Assert.True(s.TryAdd("k", "1"));
        Assert.False(s.TryAdd("k", "x"));
        Assert.True(s.TryGet("k", out var v) && v == "1");
        Assert.True(s.TryUpdate("k", "2"));
        Assert.True(s.TryRemove("k"));
    }

    [Fact]
    public void ClearAll_Really_Empty()
    {
        var s = new ShardedKeyValueStore();
        s.TryAdd("c", "1");
        s.ClearAll();
        Assert.False(s.TryGet("c", out _));
        Assert.Empty(s.Snapshot());
    }

    [Fact]
    public void Bulk_Add_Update_Delete_Reports()
    {
        var s = new ShardedKeyValueStore();
        s.TryAdd("x", "1");

        var (added, dup) = s.AddMany(new() { { "x", "9" }, { "y", "2" } });
        Assert.Equal(1, added); Assert.Single(dup);

        var (upd, abs) = s.UpdateMany(new() { { "x", "11" }, { "z", "0" } });
        Assert.Equal(1, upd); Assert.Equal(new[] { "z" }, abs);

        var (rem, miss) = s.RemoveMany(new[] { "x", "q" });
        Assert.Equal(1, rem); Assert.Equal(new[] { "q" }, miss);
    }

    [Fact]
    public void Dump_Load_RoundTrip()
    {
        var s = new ShardedKeyValueStore();
        s.TryAdd("αβ", "π");

        var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        s.Dump(tmp);

        var s2 = new ShardedKeyValueStore();
        s2.Load(tmp);

        Assert.Equal("π", s2.Snapshot()["αβ"]);
    }

    [Fact]
    public void Parallel_Adds_Are_Safe()
    {
        var s = new ShardedKeyValueStore();
        Parallel.For(0, 32, i => s.TryAdd($"p{i}", i.ToString()));
        Assert.Equal(32, s.Snapshot().Count);
    }

    [Fact]
    public void Two_Keys_SameShard_No_Interference()
    {
        string k1 = "a", k2 = null;
        var target = k1.GetHashCode() & 0b1111;
        for (int i = 0; i < 10000 && k2 == null; i++)
        {
            var c = "k" + i;
            if ((c.GetHashCode() & 0b1111) == target && c != k1) k2 = c;
        }
        Assert.NotNull(k2);

        var s = new ShardedKeyValueStore();
        Assert.True(s.TryAdd(k1, "1"));
        Assert.True(s.TryAdd(k2!, "2"));
        Assert.Equal("1", s.Snapshot()[k1]);
        Assert.Equal("2", s.Snapshot()[k2!]);
    }
}