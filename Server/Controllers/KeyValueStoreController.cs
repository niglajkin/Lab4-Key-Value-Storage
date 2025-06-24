using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("kv")]
public class KeyValueStoreController : ControllerBase
{
	private readonly ShardedKeyValueStore _store;
	public KeyValueStoreController(ShardedKeyValueStore s) => _store = s;


	[HttpPost]
	public IActionResult Set(KVPair b) =>
		_store.TryAdd(b.Key, b.Value) ? Ok()
		: Conflict($"Key already exists: {b.Key}");

	[HttpPut]
	public IActionResult Change(KVPair b) =>
		_store.TryUpdate(b.Key, b.Value) ? Ok()
		: NotFound($"Key absent: {b.Key}");

	[HttpGet("{key}")]
	public IActionResult Get(string key) =>
		_store.TryGet(key, out var v) ? Ok(v) : NotFound();

	[HttpDelete("{key}")]
	public IActionResult Delete(string key) =>
		_store.TryRemove(key) ? Ok() : NotFound();


	[HttpGet] public IActionResult GetAll() => Ok(_store.Snapshot());

	[HttpDelete]
	public IActionResult DeleteAll()
	{
		if (_store.Snapshot().Count == 0) return NoContent();
		_store.ClearAll(); return Ok();
	}

	[HttpPost("bulk")]
	public IActionResult SetMult([FromBody] Dictionary<string, string> body)
	{
		var (added, exists) = _store.AddMany(body);
		return added == 0
			? Conflict($"All keys already exist: {string.Join(", ", exists)}")
			: Ok(new { added, skipped = exists });
	}

	[HttpPut("bulk")]
	public IActionResult ChangeMult([FromBody] Dictionary<string, string> body)
	{
		var (upd, absent) = _store.UpdateMany(body);
		return upd == 0
			? NotFound($"Absent keys: {string.Join(", ", absent)}")
			: Ok(new { updated = upd, absent });
	}

	[HttpDelete("bulk")]
	public IActionResult DelMult([FromBody] string[] keys)
	{
		var (rem, absent) = _store.RemoveMany(keys);
		return rem == 0
			? NotFound($"Absent keys: {string.Join(", ", absent)}")
			: Ok(new { removed = rem, absent });
	}

	[HttpPost("dump")]
	public IActionResult Dump(DumpReq r) { _store.Dump(r.Path); return Ok(); }

	[HttpPost("load")]
	public IActionResult Load(DumpReq r)
	{
		if (!System.IO.File.Exists(r.Path))
			return NotFound("Dump file not found.");
		_store.Load(r.Path);
		return Ok("Loaded.");
	}
}

public record KVPair(string Key, string Value);
public record DumpReq(string Path);
