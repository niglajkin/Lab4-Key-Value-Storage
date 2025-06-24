using System.Net;
using System.Net.Http.Json;
using global;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _c;
    public ControllerTests(WebApplicationFactory<Program> f) => _c = f.CreateClient();

    [Fact]
    public async Task Set_Succeeds()
        => Assert.Equal(HttpStatusCode.OK,
            (await _c.PostAsJsonAsync("/kv", new { key = "a", value = "1" })).StatusCode);

    [Fact]
    public async Task Set_Duplicate_409()
    {
        await _c.PostAsJsonAsync("/kv", new { key = "dup", value = "1" });
        Assert.Equal(HttpStatusCode.Conflict,
            (await _c.PostAsJsonAsync("/kv", new { key = "dup", value = "X" })).StatusCode);
    }

    [Fact]
    public async Task Put_Update_And_404()
    {
        Assert.Equal(HttpStatusCode.NotFound,
            (await _c.PutAsJsonAsync("/kv", new { key = "nope", value = "0" })).StatusCode);

        await _c.PostAsJsonAsync("/kv", new { key = "upd", value = "1" });
        Assert.Equal(HttpStatusCode.OK,
            (await _c.PutAsJsonAsync("/kv", new { key = "upd", value = "2" })).StatusCode);

        Assert.Equal("2", await _c.GetStringAsync("/kv/upd"));
    }

    [Fact]
    public async Task Get_And_Get404()
    {
        await _c.PostAsJsonAsync("/kv", new { key = "g", value = "7" });
        Assert.Equal("7", await _c.GetStringAsync("/kv/g"));
        Assert.Equal(HttpStatusCode.NotFound, (await _c.GetAsync("/kv/missing")).StatusCode);
    }

    [Fact]
    public async Task Delete_And_Delete404()
    {
        await _c.PostAsJsonAsync("/kv", new { key = "del", value = "1" });
        Assert.Equal(HttpStatusCode.OK, (await _c.DeleteAsync("/kv/del")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _c.DeleteAsync("/kv/del")).StatusCode);
    }

    [Fact]
    public async Task GetAll_And_DeleteAll_NoContent()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _c.DeleteAsync("/kv")).StatusCode);

        await _c.PostAsJsonAsync("/kv", new { key = "x", value = "1" });
        var all = await _c.GetFromJsonAsync<Dictionary<string, string>>("/kv");
        Assert.Single(all!);

        Assert.Equal(HttpStatusCode.OK, (await _c.DeleteAsync("/kv")).StatusCode);
    }

    [Fact]
    public async Task BulkSet_And_409()
    {
        var ok = await _c.PostAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "b1", "1" }, { "b2", "2" } });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var dup = await _c.PostAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "b1", "X" } });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_And_404()
    {
        var nf = await _c.PutAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "abs", "0" } });
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);

        await _c.PostAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "bu", "1" }, { "bu2", "2" } });
        var ok = await _c.PutAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "bu", "9" } });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task BulkDelete_And_404()
    {
        var nf = await _c.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/kv/bulk")
        { Content = JsonContent.Create(new[] { "none" }) });
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);

        await _c.PostAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "bd", "1" } });
        var ok = await _c.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/kv/bulk")
        { Content = JsonContent.Create(new[] { "bd" }) });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Dump_Load_RoundTrip_And_Load404()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        await _c.PostAsJsonAsync("/kv/bulk",
            new Dictionary<string, string> { { "d", "1" } });
        await _c.PostAsJsonAsync("/kv/dump", new { path = tmp });

        var nf = await _c.PostAsJsonAsync("/kv/load", new { path = tmp + "x" });
        Assert.Equal(HttpStatusCode.NotFound, nf.StatusCode);

        await _c.DeleteAsync("/kv");
        var ok = await _c.PostAsJsonAsync("/kv/load", new { path = tmp });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        Assert.Equal("1", await _c.GetStringAsync("/kv/d"));
    }
}