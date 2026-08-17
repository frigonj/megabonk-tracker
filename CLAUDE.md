# Megabonk Tracker

## Commands

```bash
# Build & deploy the BepInEx plugin (auto-copies DLL into the game's plugins folder)
cd plugin
dotnet build -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Megabonk"

# Dashboard: install deps
cd dashboard
python -m venv .venv
.venv\Scripts\pip install -r requirements.txt

# Dashboard: run
.venv\Scripts\python main.py
# -> http://127.0.0.1:8420 (live view), /history (cross-run stats)
```

No test suite or linter configured yet — this is a personal tool, verification is done by playing the game and checking the dashboard/logs.

## Stack

- **Plugin** (`plugin/`): C#, BepInEx IL2CPP (`net6.0`), HarmonyLib for runtime patches into the game process
- **Dashboard** (`dashboard/`): Python, FastAPI + Jinja2 + WebSocket, SQLite (stdlib `sqlite3`, no ORM)
- **Charts**: hand-rolled SVG line chart (`dashboard/static/chart.js`), no charting library — follows the `dataviz` skill's method (validated categorical palette, hover crosshair/tooltip, direct-labeled top series + "Other" fold)

## Conventions

- The plugin never writes anything but a local newline-delimited JSON event file (`%APPDATA%/MegabonkTracker/live_events.ndjson`) — no network calls from inside the game process, by design (see README "How it works").
- JSON serialization in the plugin is hand-written (`SimpleJson.cs`), not Newtonsoft — BepInEx does not deploy NuGet dependencies alongside a plugin DLL, so any added dependency needs its own deploy step. Keep it dependency-free unless that's solved.
- `dashboard/tracker.py` is the single source of truth for turning the raw event stream into both live WebSocket state and SQLite writes — event types are handled in `_handle_event`, one `elif` per plugin event type.
- Game class names (`GameManager`, `UpgradePicker`, `RunStats`, `WeaponBase`, `WeaponData`, `PlayerInventory`, `EStat`, `StatModifier`) were found by decompiling the game's BepInEx-generated IL2CPP interop assemblies with `ilspycmd`, not by guessing — if a game update breaks the plugin, re-decompile `BepInEx/interop/Assembly-CSharp.dll` and diff against `plugin/Plugin.cs`'s Harmony patch targets.

## Development Environment

This project is Windows-native by necessity (Steam game, BepInEx, `dotnet build` targeting the game's own IL2CPP interop DLLs at a Windows path) — PowerShell and Windows paths are the default here, not bash, overriding the global convention.

## Architecture Notes

- **Plugin → Dashboard transport**: the plugin writes NDJSON to a local file; `dashboard/tracker.py` tails it on a poll loop (not a filesystem watch) and truncation (new run started) is detected by the file size shrinking.
- **Live vs. persisted state**: `tracker.state` (in-memory) drives the live WebSocket view; every event is also written to SQLite (`dashboard/runs.db`) as it arrives, so a completed run is queryable via `/history` without needing the live process to still be running.
- **Stat tracking**: `RunStats.damageSources` (a live `Dictionary<string, DamageSource>` on the game's own static class) is the anchor for per-weapon damage — polled once/sec. Per-weapon *mechanic* stats (crit chance, attack speed, etc.) come from `WeaponBase.GetValue(EStat)` / `WeaponData.GetBaseStat(EStat)`, polled the same cadence, capped at whatever weapons are currently equipped (game only allows ~4 at once, so this is cheap).
- **Combo analysis** (`db.py: combo_stats`) computes item/tome pair and triple co-occurrence on read from `picks` + `runs` — no precomputed combo table. Needs a nontrivial number of completed runs before results are statistically meaningful; the `sample_size` column is the signal for that.
