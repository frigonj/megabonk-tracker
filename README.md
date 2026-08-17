# Megabonk Tracker

A live damage and build tracker for [Megabonk](https://store.steampowered.com/app/3405340/Megabonk/). Tracks a run's damage output, item/tome/weapon picks, shrine and encounter buffs, and player/weapon stats, and keeps a local history so you can compare builds across runs — all from data pulled directly out of the running game, no external services or lookups involved.

## How it works

Megabonk is a Unity/IL2CPP game. This project has two parts:

- **`plugin/`** — a [BepInEx](https://docs.bepinex.dev/) IL2CPP plugin that hooks into the game process (via [Harmony](https://github.com/pardeike/Harmony) patches on `GameManager`, `UpgradePicker`, `EffectStat`, and `PauseUi`) and writes a stream of run events to a local newline-delimited JSON file.
- **`dashboard/`** — a local FastAPI + WebSocket web app that tails that file, shows a live view while you play, and persists every completed run to a local SQLite database for cross-run analysis (which items/tomes/combinations correlate with higher damage output).

Nothing here talks to the network. All data comes from the game's own in-memory state and stays on your machine.

**Live vs. end-of-run:** damage totals and run counters (gold, level, etc.) update every second while you play. Weapon and player stat breakdowns are captured once, right when the run ends — walking the game's full stat catalog every second turned out to cause a noticeable in-game stutter, and the build-analysis use case only needs the end-of-run numbers anyway.

## Setup

### Prerequisites

- Megabonk installed via Steam
- [BepInEx IL2CPP](https://builds.bepinex.dev/projects/bepinex_be) (bleeding-edge build) installed into the game folder — extract the `win-x64` IL2CPP zip into the Megabonk install directory so `BepInEx/core/BepInEx.Unity.IL2CPP.dll` exists
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (to build the plugin)
- Python 3.10+ (to run the dashboard)

### Build and install the plugin

```
cd plugin
dotnet build -p:GameDir="C:\Path\To\Your\Megabonk"
```

The build automatically copies `MegabonkTracker.dll` into `BepInEx/plugins/MegabonkTracker/` in your game folder. Launch Megabonk once — BepInEx will load the plugin, and `BepInEx/LogOutput.log` should show `MegabonkTracker loaded.`

### Run the dashboard

```
cd dashboard
python -m venv .venv
.venv\Scripts\pip install -r requirements.txt
.venv\Scripts\python main.py
```

Open `http://127.0.0.1:8420` while a run is in progress for the live view, or `/history` for cross-run stats.

## What it tracks

- **Live damage over time** — total and per-weapon damage curves, polled once per second from the game's own `RunStats.damageSources`
- **Picks** — every item/tome/weapon upgrade chosen at level-up, with the exact stat modifiers it applied
- **Shrine, gravestone, and encounter-reward effects** — buffs/debuffs from sources other than level-up picks, with the stat changed, amount, and whether it's permanent or timed (see caveat below — coverage isn't confirmed for every shrine type yet)
- **Weapon and player stats** — base stats vs. final effective stats, captured once at the end of the run, for both individual weapons and player-wide stats (crit chance, move speed, luck, etc. — the stats tomes and some items modify that aren't tied to one weapon)
- **Run counters** — gold, character level, and banishes/refreshes/skips used, live throughout the run
- **Pause-aware timing** — the plugin detects the pause menu and stops emitting snapshots while paused; the dashboard excludes paused wall-clock time from the damage-over-time chart and from run duration/average DPS, so pausing to grab a coffee doesn't distort either
- **Cross-run history** — per-item pick rate and average DPS, plus pairwise/triple item-combination analysis to help spot synergies (each combo's sample size is shown, since small sample sizes aren't statistically meaningful)

## Notes

This relies on reading the game's internal class structure via BepInEx's IL2CPP interop layer, decompiled from the installed game build. A Megabonk update that renames or restructures the classes this plugin hooks (`GameManager`, `UpgradePicker`, `RunStats`, `WeaponBase`, `PlayerInventory`, `EffectStat`, `PauseUi`) could break it until the plugin is updated to match.

Shrine/gravestone/encounter effects all route through one shared game class (`EffectStat.ApplyEffect`), confirmed working for encounter-reward screens. Coverage for every individual shrine type (Greed, Balance, Cursed, Magnet, Moai, Challenge) isn't fully verified yet — some may apply their effects a different way that this plugin doesn't currently catch. If a shrine's effect doesn't show up in the tracker, that's the likely reason.
