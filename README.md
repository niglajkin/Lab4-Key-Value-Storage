# Lab 4 — In‑Memory Key‑Value Storage

This repository contains a **C#/.NET 8** in‑memory sharded key‑value storage (“Server”) together with a small **Python 3** console client app (“Client”).  

The project is intentionally lightweight: it shows how to expose a minimal CRUD + bulk REST API on the server side and drive it from any language that can issue HTTP requests.

---

## Requirements

| Tool | Minimum version | Purpose |
|------|-----------------|---------|
| [.NET SDK](https://visualstudio.microsoft.com/en/downloads/) | **8.0** | Build & run the C# web‑API |
| [Python](https://www.python.org/downloads/) | **3.8** (tested on 3.12) | Run the interactive CLI client |

---

## Building & running the **Server**

```bash
dotnet build
dotnet run --project Server
```
---

### When both are running, use CLI Commands

| Command | Syntax | Effect |
|---------|--------|--------|
| **SET** | `SET key value` | Insert `key → value`; fails if key exists |
| **CHANGE** | `CHANGE key value` | Update existing `key` to `value` |
| **SETMULT** | `SETMULT {a:1 b:2}` | Bulk insert; skips duplicates |
| **CHANGEMULT** | `CHANGEMULT {a:10 b:20}` | Bulk update; reports absent keys |
| **GET** | `GET key` | Print value or **NOT FOUND** |
| **GETALL** | `GETALL` | Pretty‑print entire store |
| **DELETE** | `DELETE key` | Remove single key |
| **DELMULT** | `DELMULT {a b c}` | Remove several keys |
| **DELETEALL** | `DELETEALL` | Clear store |
| **DUMP** | `DUMP path.json` | Save snapshot to JSON |
| **LOAD** | `LOAD path.json` | Load snapshot (overwrites store) |
| **EXIT** | `EXIT` | Quit (offers to dump first if data present) |

